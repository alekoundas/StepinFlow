using Business.Flows.DataService;
using Business.FlowScript.Diagnostics;
using Business.FlowScript.Models;
using Business.FlowScript.Scanner;
using Core.Enums;
using Core.Models.Database;
using Core.Models.Dtos.FlowScript;

namespace Business.FlowScript
{
    /// <summary>
    /// A look at a .sflw file before it is imported. It is read from disk and scanned the way the
    /// importer does both, and the flow it would replace is found the way the import's write path
    /// finds it, by the file's Id, so what this says is what an import then does. Nothing is saved.
    /// </summary>
    public sealed class FlowScriptPreviewService : IFlowScriptPreviewService
    {
        private readonly IScanner _scanner;
        private readonly IFlowScriptExporter _exporter;
        private readonly DataService _dataService;

        public FlowScriptPreviewService(IScanner scanner, IFlowScriptExporter exporter, DataService dataService)
        {
            _scanner = scanner;
            _exporter = exporter;
            _dataService = dataService;
        }


        // ================================================================
        // Public methods
        // ================================================================

        public async Task<FlowScriptPreviewDto> PreviewAsync(string scriptPath, CancellationToken ct = default)
        {
            FlowScriptPreviewDto preview = new FlowScriptPreviewDto { ScriptPath = scriptPath };

            if (!File.Exists(scriptPath))
            {
                Diagnostic missing = Diagnostic.Error(DiagnosticCodeEnum.FILE_MISSING, 1, 1, $"There is no file at {scriptPath}.");
                preview.Errors = FlowScriptImporter.ToDtos([missing], DiagnosticSeverityEnum.ERROR);
                return preview;
            }

            FlowScriptFile file = await FlowScriptFile.ReadAsync(scriptPath, ct);
            FlowScriptSchema schema = _scanner.Read(file.Script);

            // The sub-flows the import would not find are warned about before it, not after.
            List<Diagnostic> unlinked = await _dataService.Flow.LinkSubFlowsAsync(schema, ct);
            schema.Diagnostics.AddRange(unlinked);

            preview.Script = file.Script;
            preview.TemplateFolderPath = file.TemplateFolderPath;
            preview.Errors = FlowScriptImporter.ToDtos(schema.Diagnostics, DiagnosticSeverityEnum.ERROR);
            preview.Warnings = FlowScriptImporter.ToDtos(schema.Diagnostics, DiagnosticSeverityEnum.WARNING);

            preview.FlowName = schema.Flow.Name;
            preview.PublicId = schema.Flow.PublicId;
            preview.Description = schema.Flow.Description;
            preview.StepCount = schema.Steps.Count(x => x.FlowStepType != FlowStepTypeEnum.SUCCESS && x.FlowStepType != FlowStepTypeEnum.FAILURE);
            preview.Inputs = schema.Inputs.Select(x => InputOf(x)).ToList();

            preview.TemplateNames = schema.Steps
                .SelectMany(x => x.FlowStepTemplates)
                .Select(x => x.FlowTemplate.Name)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
            preview.MissingTemplates = preview.TemplateNames.Where(x => !file.Templates.ContainsKey(x)).ToList();

            // A file that did not get as far as its name and Id meets no flow.
            if (preview.FlowName.Length == 0 || preview.PublicId == Guid.Empty)
                return preview;

            preview.Target = await _dataService.Flow.GetImportTargetAsync(preview.PublicId, preview.FlowName, ct);
            if (preview.Target.FlowId != null)
                preview.ReplacedScript = await _exporter.RenderAsync(preview.Target.FlowId.Value, ct);

            return preview;
        }


        // ================================================================
        // Private methods
        // ================================================================

        private static FlowScriptInputDto InputOf(FlowCsvColumn input)
        {
            return new FlowScriptInputDto
            {
                Name = input.Name,
                DefaultValue = input.DefaultValue,
                IsSecret = input.IsSecret,
            };
        }
    }
}
