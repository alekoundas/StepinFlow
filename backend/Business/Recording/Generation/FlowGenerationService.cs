using Business.FlowScript;
using Business.FlowScript.Models;
using Business.FlowScript.Text;
using Business.Recording.Draft;
using Core.Models.Database;
using Core.Models.Dtos;
using Core.Models.Dtos.FlowScript;

namespace Business.Recording.Generation
{
    /// <summary>
    /// A recording into a flow: the draft, the script, the import. The draft goes through the
    /// printer and the scanner rather than straight to the database, so a recorded flow, a file and
    /// a model's answer all land the one way, and the plain draft is held to what a file is.
    /// </summary>
    public sealed class FlowGenerationService : IFlowGenerationService
    {
        private readonly IFlowScriptExporter _exporter;
        private readonly IFlowScriptImporter _importer;
        private readonly IRecordingDraftBuilder _draftBuilder;
        private readonly IPrinter _printer;

        public FlowGenerationService(IFlowScriptExporter exporter, IFlowScriptImporter importer, IRecordingDraftBuilder draftBuilder, IPrinter printer)
        {
            _exporter = exporter;
            _importer = importer;
            _draftBuilder = draftBuilder;
            _printer = printer;
        }


        // ================================================================
        // Public methods
        // ================================================================

        public async Task<FlowScriptImportResultDto> ImportPlainDraftAsync(
            int flowId,
            RecordingResultDto recording,
            ReviewedRecordingDto reviewed,
            Func<int, RecordingScreenDto?> screenAt,
            CancellationToken ct = default)
        {
            FlowScriptSchema flow = await _exporter.LoadAsync(flowId, ct);
            FlowScriptSchema draft = _draftBuilder.Build(flow, recording, reviewed, screenAt);

            string script = _printer.Write(draft);
            Dictionary<string, byte[]> templates = TemplatesOf(draft);

            return await _importer.ImportTextAsync(script, templates, ct);
        }


        // ================================================================
        // Private methods
        // ================================================================

        // By name, as an import reads them: the ones cut from the recording and the ones the flow had.
        private static Dictionary<string, byte[]> TemplatesOf(FlowScriptSchema draft)
        {
            Dictionary<string, byte[]> templates = new Dictionary<string, byte[]>(StringComparer.OrdinalIgnoreCase);

            foreach (FlowStepTemplate template in draft.Steps.SelectMany(x => x.FlowStepTemplates))
            {
                if (template.TemplateImage != null)
                    templates[template.Name] = template.TemplateImage;
            }

            return templates;
        }
    }
}
