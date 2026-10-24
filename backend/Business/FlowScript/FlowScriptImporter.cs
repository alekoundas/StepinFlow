using Core.Models.Database;
using Core.Models.Dtos;

using Business.Flows.DataService;
using Business.FlowScript.Diagnostics;
using Business.FlowScript.Models;
using Business.FlowScript.Scanner;
using Core.Models.Dtos.FlowScript;

namespace Business.FlowScript
{
    /// <summary>
    /// Import .sflw file into the database. The mirror of FlowScriptExporter.cs.
    /// Parse, take each template's image by its name, then replace the flow's rows through its data service.
    ///
    /// DOESNT delete the old Flow, just its rows: the Flow stays for its execution history.
    /// </summary>
    public sealed class FlowScriptImporter : IFlowScriptImporter
    {
        private readonly IScanner _scanner;
        private readonly DataService _dataService;

        public FlowScriptImporter(IScanner scanner, DataService dataService)
        {
            _scanner = scanner;
            _dataService = dataService;
        }


        // ================================================================
        // Public methods
        // ================================================================

        /// <summary>
        /// Import Flow Script. Existing flow gets replaced by the imported one.
        /// </summary>
        public async Task<FlowScriptImportResultDto> ImportAsync(string scriptPath, CancellationToken ct = default)
        {
            if (!File.Exists(scriptPath))
                return Failed([Diagnostic.Error(DiagnosticCodeEnum.FILE_MISSING, 1, 1, $"There is no file at {scriptPath}.")]);

            FlowScriptFile file = await FlowScriptFile.ReadAsync(scriptPath, ct);

            return await ImportTextAsync(file.Script, file.Templates, ct);
        }

        /// <summary>
        /// Import Flow Script. Existing flow gets replaced by the imported one.
        /// </summary>
        public async Task<FlowScriptImportResultDto> ImportTextAsync(string script, IReadOnlyDictionary<string, byte[]> templates, CancellationToken ct = default)
        {

            FlowScriptSchema schema = _scanner.Read(script); // Script -> ScriptLines -> LineTokens -> Parse LineTokens -> Schema
            if (!schema.IsValid)
                return Failed(schema.Diagnostics);

            FlowScriptImportResultDto result = new FlowScriptImportResultDto
            {
                IsSuccess = true,
                FlowName = schema.Flow.Name,
                StepCount = schema.Steps.Count,
                Warnings = ToDtos(schema.Diagnostics, DiagnosticSeverityEnum.WARNING),
            };

            foreach (FlowTemplate template in schema.Steps.SelectMany(x => x.FlowStepTemplates).Select(x => x.FlowTemplate).Distinct())
                TakeTemplateImage(template, templates, result);

            // Save to Database.
            ResultDto<(int FlowId, FlowValidationResultDto Validation)> replaced = await _dataService.Flow.ReplaceAsync(schema, ct);
            if (!replaced.IsSuccess)
                return Failed([Diagnostic.Error(DiagnosticCodeEnum.FLOW_NAME_TAKEN, 1, 1, replaced.ErrorMessage ?? string.Empty)]);

            result.FlowId = replaced.Data.FlowId;
            result.Validation = replaced.Data.Validation;

            return result;
        }


        // ================================================================
        // Private methods
        // ================================================================

        private static void TakeTemplateImage(FlowTemplate template, IReadOnlyDictionary<string, byte[]> templates, FlowScriptImportResultDto result)
        {
            // A missing image is reported, and the template is kept without one.
            templates.TryGetValue(template.Name, out byte[]? image);
            if (image == null)
                result.MissingTemplates.Add(template.Name);

            template.TemplateImage = image;

            result.TemplateCount++;
        }

        // Also what the recording's draft says when it does not read as a file.
        internal static FlowScriptImportResultDto Failed(IReadOnlyList<Diagnostic> diagnostics)
        {
            return new FlowScriptImportResultDto
            {
                IsSuccess = false,
                Errors = ToDtos(diagnostics, DiagnosticSeverityEnum.ERROR),
                Warnings = ToDtos(diagnostics, DiagnosticSeverityEnum.WARNING),
            };
        }

        internal static List<FlowScriptErrorDto> ToDtos(IReadOnlyList<Diagnostic> diagnostics, DiagnosticSeverityEnum severity)
        {
            return diagnostics
                .Where(x => x.Severity == severity)
                .Select(x => new FlowScriptErrorDto
                {
                    Line = x.Line,
                    Column = x.Column,
                    Message = x.Message,
                    Code = x.Code.ToString(),
                    Severity = x.Severity.ToString(),
                })
                .ToList();
        }
    }
}
