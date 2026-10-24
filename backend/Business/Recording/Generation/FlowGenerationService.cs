using Business.Ai.Helpers;
using Business.Ai.Providers;
using Business.Ai.ScriptRevision;
using Business.Ai.ScriptRevision.Models;
using Business.Flows.DataService;
using Business.FlowScript;
using Business.FlowScript.Diagnostics;
using Business.FlowScript.Models;
using Business.FlowScript.Scanner;
using Business.FlowScript.Text;
using Business.Recording.Draft;
using Core.Enums;
using Core.Models.Database;
using Core.Models.Dtos;
using Core.Models.Dtos.FlowScript;

namespace Business.Recording.Generation
{
    /// <summary>
    /// A recording into a flow: the draft, the script, the import. The draft goes through the
    /// printer and the scanner rather than straight to the database, so a recorded flow, a file and
    /// a model's answer all land the one way, and the plain draft is held to what a file is.
    ///
    /// Into a flow that already existed, at a position, it is read back the same way but only the
    /// new rows are written: the import replaces every row, and a step replaced loses its last good
    /// screenshots and its link to the executions it was in.
    ///
    /// With a model, the plain draft is what it is given, as a script with the description, and its
    /// answer is imported in place of it. When no answer reads cleanly, or the model cannot be
    /// reached, the plain draft is saved instead and says why, so the tester always has a flow.
    ///
    /// On the Check page the tester can ask for changes: the flow as saved goes to the model with
    /// what they wrote, and an answer that never reads cleanly leaves the flow as it was.
    /// </summary>
    public sealed class FlowGenerationService : IFlowGenerationService
    {
        private readonly IFlowScriptExporter _exporter;
        private readonly IFlowScriptImporter _importer;
        private readonly IRecordingDraftBuilder _draftBuilder;
        private readonly IPrinter _printer;
        private readonly IScanner _scanner;
        private readonly DataService _dataService;
        private readonly IScriptRevisionService _revisionService;
        private readonly IAiProviderService _providerService;

        public FlowGenerationService(
            IFlowScriptExporter exporter,
            IFlowScriptImporter importer,
            IRecordingDraftBuilder draftBuilder,
            IPrinter printer,
            IScanner scanner,
            DataService dataService,
            IScriptRevisionService revisionService,
            IAiProviderService providerService)
        {
            _exporter = exporter;
            _importer = importer;
            _draftBuilder = draftBuilder;
            _printer = printer;
            _scanner = scanner;
            _dataService = dataService;
            _revisionService = revisionService;
            _providerService = providerService;
        }


        // ================================================================
        // Public methods
        // ================================================================

        public async Task<FlowGenerationResultDto> ImportPlainDraftAsync(
            int flowId,
            RecordingResultDto recording,
            ReviewedRecordingDto reviewed,
            Func<int, RecordingScreenDto?> screenAt,
            CancellationToken ct = default)
        {
            FlowGenerationResultDto result = new FlowGenerationResultDto();
            result.Import = await ImportPlainAsync(flowId, recording, reviewed, screenAt, ct);

            await ReadSavedAsync(result, ct);
            return result;
        }

        public async Task<ResultDto<FlowScriptImportResultDto>> InsertPlainDraftAsync(
            RecordingDraftPositionDto position,
            RecordingResultDto recording,
            ReviewedRecordingDto reviewed,
            Func<int, RecordingScreenDto?> screenAt,
            CancellationToken ct = default)
        {
            FlowScriptSchema flow = await _exporter.LoadAsync(position.FlowId, ct);

            FlowStep? under = null;
            if (position.ParentFlowStepId != null)
            {
                under = flow.Steps.FirstOrDefault(x => x.Id == position.ParentFlowStepId);
                if (under == null)
                    return ResultDto<FlowScriptImportResultDto>.Failure("The step the recording goes under is no longer in the flow.");
            }

            HashSet<FlowStep> stepsBefore = flow.Steps.ToHashSet();
            HashSet<FlowCsvColumn> inputsBefore = flow.Inputs.ToHashSet();

            FlowScriptSchema draft = _draftBuilder.Build(flow, recording, reviewed, screenAt, under, position.Index);

            string script = _printer.Write(draft);
            FlowScriptSchema read = _scanner.Read(script);
            if (!read.IsValid)
            {
                FlowScriptImportResultDto refused = FlowScriptImporter.Failed(read.Diagnostics);
                return ResultDto<FlowScriptImportResultDto>.Success(refused);
            }

            List<FlowStep> steps = draft.Steps.Where(x => !stepsBefore.Contains(x)).ToList();
            List<FlowCsvColumn> inputs = draft.Inputs.Where(x => !inputsBefore.Contains(x)).ToList();

            // The tree insert hangs them at the position by its ids, as it renumbers the steps there.
            foreach (FlowStep step in steps.Where(x => x.ParentFlowStep == under))
                step.ParentFlowStep = null;

            ResultDto<int> saved = await _dataService.FlowStep.CreateTreeAsync(position.FlowId, position.ParentFlowStepId, position.Index, steps, inputs, ct);
            if (!saved.IsSuccess)
                return ResultDto<FlowScriptImportResultDto>.Failure(saved.ErrorMessage ?? "The recording could not be written into the flow.");

            FlowScriptImportResultDto inserted = new FlowScriptImportResultDto
            {
                IsSuccess = true,
                FlowId = saved.Data,
                FlowName = flow.Flow.Name,
                StepCount = steps.Count,
                TemplateCount = steps.SelectMany(x => x.FlowStepTemplates).Select(x => x.FlowTemplate).Distinct().Count(),
            };

            return ResultDto<FlowScriptImportResultDto>.Success(inserted);
        }

        public async Task<FlowGenerationResultDto> ImportModelDraftAsync(
            int flowId,
            RecordingResultDto recording,
            ReviewedRecordingDto reviewed,
            Func<int, RecordingScreenDto?> screenAt,
            Action<FlowGenerationProgressDto>? onProgress,
            CancellationToken ct = default)
        {
            bool isReadTextSent = await _providerService.CanSendScreenDataAsync(ct);
            string model = await _providerService.GetModelAsync(ct);

            FlowGenerationProgressDto writing = new FlowGenerationProgressDto
            {
                State = FlowGenerationStateEnum.GENERATING,
                Stage = FlowGenerationStageEnum.WRITING_DRAFT,
                Model = model,
                IsReadTextSent = isReadTextSent,
                AttemptCount = ScriptRevisionService.ATTEMPT_COUNT,
            };
            onProgress?.Invoke(writing);

            // Withheld, the steps get placeholder names, and the model names them from the description.
            RecordingResultDto sent = recording;
            if (!isReadTextSent)
                sent = WithoutReadText(recording);

            FlowScriptSchema flow = await _exporter.LoadAsync(flowId, ct);
            FlowScriptSchema draft = _draftBuilder.Build(flow, sent, reviewed, screenAt, null, null);

            string script = _printer.Write(draft);
            Dictionary<string, byte[]> templates = TemplatesOf(draft);

            FlowGenerationProgressDto drafted = Next(writing, FlowGenerationStageEnum.WRITING_DRAFT, 0, []);
            drafted.ScriptLength = script.Length;

            string instruction = AiPromptHelper.ShapeRecording;
            if (!isReadTextSent)
                instruction += $"\n\n{AiPromptHelper.ShapeRecordingWithoutReadText}";

            ScriptRevisionResult revision = await _revisionService.ReviseAsync(script, instruction, (attempt, refused) =>
            {
                FlowGenerationProgressDto asking = Next(drafted, FlowGenerationStageEnum.ASKING_MODEL, attempt, refused);
                onProgress?.Invoke(asking);
            }, ct);

            FlowGenerationProgressDto saving = Next(drafted, FlowGenerationStageEnum.SAVING, revision.Attempts, []);
            onProgress?.Invoke(saving);

            FlowGenerationResultDto result = new FlowGenerationResultDto
            {
                Notes = revision.Notes,
                Attempts = revision.Attempts,
                Request = revision.Request,
            };

            if (revision.Script != null)
            {
                result.IsModelDraft = true;
                result.Draft = script;
                result.Import = await _importer.ImportTextAsync(revision.Script, templates, ct);
            }
            else
            {
                // Built again from the recording itself, so its names have the text read under each click.
                result.Reason = revision.ErrorMessage ?? $"No answer from the model read cleanly in {revision.Attempts} attempts.";
                result.Refused = FlowScriptImporter.ToDtos(revision.Refused, DiagnosticSeverityEnum.ERROR);
                result.Import = await ImportPlainAsync(flowId, recording, reviewed, screenAt, ct);
            }

            await ReadSavedAsync(result, ct);
            return result;
        }

        public async Task<FlowGenerationResultDto> ReviseAsync(
            int flowId,
            string changes,
            Action<FlowGenerationProgressDto>? onProgress,
            CancellationToken ct = default)
        {
            string model = await _providerService.GetModelAsync(ct);

            // The script is the flow as the tester has read it, so it goes as it is.
            FlowGenerationProgressDto reading = new FlowGenerationProgressDto
            {
                State = FlowGenerationStateEnum.GENERATING,
                Stage = FlowGenerationStageEnum.WRITING_DRAFT,
                Model = model,
                IsReadTextSent = true,
                AttemptCount = ScriptRevisionService.ATTEMPT_COUNT,
            };
            onProgress?.Invoke(reading);

            string script = await _exporter.RenderAsync(flowId, ct);
            FlowScriptSchema flow = await _exporter.LoadAsync(flowId, ct);
            Dictionary<string, byte[]> templates = TemplatesOf(flow);

            FlowGenerationProgressDto read = Next(reading, FlowGenerationStageEnum.WRITING_DRAFT, 0, []);
            read.ScriptLength = script.Length;

            string instruction = $"{AiPromptHelper.ReviseOnRequest}\n\nWhat the tester asks for:\n\n{changes.Trim()}";

            ScriptRevisionResult revision = await _revisionService.ReviseAsync(script, instruction, (attempt, refused) =>
            {
                FlowGenerationProgressDto asking = Next(read, FlowGenerationStageEnum.ASKING_MODEL, attempt, refused);
                onProgress?.Invoke(asking);
            }, ct);

            FlowGenerationResultDto result = new FlowGenerationResultDto
            {
                Notes = revision.Notes,
                Attempts = revision.Attempts,
                Request = revision.Request,
                Draft = script,
            };

            if (revision.Script == null)
            {
                result.Reason = revision.ErrorMessage ?? $"No answer from the model read cleanly in {revision.Attempts} attempts, so the flow is as it was.";
                result.Refused = FlowScriptImporter.ToDtos(revision.Refused, DiagnosticSeverityEnum.ERROR);
                return result;
            }

            FlowGenerationProgressDto saving = Next(read, FlowGenerationStageEnum.SAVING, revision.Attempts, []);
            onProgress?.Invoke(saving);

            result.IsModelDraft = true;
            result.Import = await _importer.ImportTextAsync(revision.Script, templates, ct);

            await ReadSavedAsync(result, ct);
            return result;
        }


        // ================================================================
        // Private methods
        // ================================================================

        private async Task<FlowScriptImportResultDto> ImportPlainAsync(
            int flowId,
            RecordingResultDto recording,
            ReviewedRecordingDto reviewed,
            Func<int, RecordingScreenDto?> screenAt,
            CancellationToken ct)
        {
            FlowScriptSchema flow = await _exporter.LoadAsync(flowId, ct);
            FlowScriptSchema draft = _draftBuilder.Build(flow, recording, reviewed, screenAt, null, null);

            string script = _printer.Write(draft);
            Dictionary<string, byte[]> templates = TemplatesOf(draft);

            return await _importer.ImportTextAsync(script, templates, ct);
        }

        // The flow as saved, read back from the database rather than taken from what was sent to it.
        private async Task ReadSavedAsync(FlowGenerationResultDto result, CancellationToken ct)
        {
            if (!result.Import.IsSuccess)
                return;

            result.Script = await _exporter.RenderAsync(result.Import.FlowId, ct);
            FlowScriptSchema saved = _scanner.Read(result.Script);

            result.StepCount = saved.Steps.Count(x => x.FlowStepType != FlowStepTypeEnum.SUCCESS && x.FlowStepType != FlowStepTypeEnum.FAILURE);
            result.InputCount = saved.Inputs.Count;
            result.TemplateCount = saved.Steps
                .SelectMany(x => x.FlowStepTemplates)
                .Select(x => x.FlowTemplate.Name)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Count();
        }

        // By name, as an import reads them: the ones cut from the recording and the ones the flow had.
        private static Dictionary<string, byte[]> TemplatesOf(FlowScriptSchema draft)
        {
            Dictionary<string, byte[]> templates = new Dictionary<string, byte[]>(StringComparer.OrdinalIgnoreCase);

            foreach (FlowTemplate template in draft.Steps.SelectMany(x => x.FlowStepTemplates).Select(x => x.FlowTemplate))
            {
                if (template.TemplateImage != null)
                    templates[template.Name] = template.TemplateImage;
            }

            return templates;
        }

        // The same recording with nothing read under its clicks.
        private static RecordingResultDto WithoutReadText(RecordingResultDto recording)
        {
            List<ClickedElementDto> elements = recording.ClickedElements
                .Select(x => new ClickedElementDto
                {
                    ActionIndex = x.ActionIndex,
                    X = x.X,
                    Y = x.Y,
                    Width = x.Width,
                    Height = x.Height,
                    FoundBy = x.FoundBy,
                    Note = x.Note,
                    Image = x.Image,
                })
                .ToList();

            return new RecordingResultDto
            {
                Actions = recording.Actions,
                ClickedElements = elements,
                ClickGroups = recording.ClickGroups,
            };
        }

        // A new one each time, since the last may still be on its way to the page.
        private static FlowGenerationProgressDto Next(FlowGenerationProgressDto last, FlowGenerationStageEnum stage, int attempt, IReadOnlyList<Diagnostic> refused)
        {
            return new FlowGenerationProgressDto
            {
                State = last.State,
                Stage = stage,
                Model = last.Model,
                IsReadTextSent = last.IsReadTextSent,
                ScriptLength = last.ScriptLength,
                Attempt = attempt,
                AttemptCount = last.AttemptCount,
                Refused = FlowScriptImporter.ToDtos(refused, DiagnosticSeverityEnum.ERROR),
            };
        }
    }
}
