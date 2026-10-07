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
    /// Parse, read the template files, then replace the flow's rows through its data service.
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
                return Failed(Diagnostic.Error(DiagnosticCodeEnum.FILE_MISSING, 1, 1, $"There is no file at {scriptPath}."));

            string script = await File.ReadAllTextAsync(scriptPath, ct);

            // Templates sit in a folder named after the file, beside it.
            string templateFolderPath = string.Empty;
            string? directory = Path.GetDirectoryName(scriptPath);
            if (directory != null)
                templateFolderPath = Path.Combine(directory, Path.GetFileNameWithoutExtension(scriptPath));

            return await ImportTextAsync(script, templateFolderPath, ct);
        }

        /// <summary>
        /// Import Flow Script. Existing flow gets replaced by the imported one.
        /// </summary>
        public async Task<FlowScriptImportResultDto> ImportTextAsync(string script, string? templateFolderPath, CancellationToken ct = default)
        {

            FlowScriptSchema schema = _scanner.Read(script); // Script -> ScriptLines -> LineTokens -> Parse LineTokens -> Schema
            if (!schema.IsValid)
                return Failed(schema.Diagnostics);

            FlowScriptImportResultDto result = new FlowScriptImportResultDto
            {
                IsSuccess = true,
                FlowName = schema.Flow.Name,
                StepCount = schema.Steps.Count,
            };

            // Read template image bytes from disk.
            foreach (FlowStepTemplate template in schema.Steps.SelectMany(x => x.FlowStepTemplates))
                await ReadTemplateBytesAsync(template, templateFolderPath, result, ct);

            // Save to Database.
            ResultDto<(int FlowId, FlowValidationResultDto Validation)> replaced = await _dataService.Flow.ReplaceAsync(schema, ct);

            result.FlowId = replaced.Data.FlowId;
            result.Validation = replaced.Data.Validation;

            return result;
        }


        // ================================================================
        // Private methods
        // ================================================================

        private static async Task ReadTemplateBytesAsync(FlowStepTemplate template, string? templateFolderPath, FlowScriptImportResultDto result, CancellationToken ct)
        {
            string fileName = template.Name;

            // A missing image is reported.
            byte[]? image = null;
            if (templateFolderPath != null)
            {
                string path = Path.Combine(templateFolderPath, fileName);
                if (File.Exists(path))
                    image = await File.ReadAllBytesAsync(path, ct);
            }

            // Add missing .png file names to report.
            if (image == null)
                result.MissingTemplates.Add(fileName);

            template.Name = Path.GetFileNameWithoutExtension(fileName);
            template.TemplateImage = image;

            result.TemplateCount++;
        }

        private static FlowScriptImportResultDto Failed(Diagnostic error)
        {
            List<Diagnostic> diagnostics = new List<Diagnostic>();
            diagnostics.Add(error);
            return Failed(diagnostics);
        }

        private static FlowScriptImportResultDto Failed(IReadOnlyList<Diagnostic> errors)
        {
            return new FlowScriptImportResultDto
            {
                IsSuccess = false,
                Errors = errors
                    .Select(x => new FlowScriptErrorDto
                    {
                        Line = x.Line,
                        Column = x.Column,
                        Message = x.Message,
                        Code = x.Code.ToString(),
                        Severity = x.Severity.ToString(),
                    })
                    .ToList(),
            };
        }
    }
}
