using System.Buffers.Binary;
using System.Drawing;

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
        private static readonly byte[] PngSignature = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];

        private readonly IScanner _scanner;
        private readonly DataService _dataService;

        public FlowScriptImporter(IScanner reader, DataService dataService)
        {
            _scanner = reader;
            _dataService = dataService;
        }


        // ================================================================
        // Public methods
        // ================================================================

        public async Task<FlowImportResultDto> ImportAsync(string scriptPath, CancellationToken ct = default)
        {
            if (!File.Exists(scriptPath))
                return Failed(Diagnostic.Error(DiagnosticCodeEnum.FILE_MISSING, 1, 1, $"There is no file at {scriptPath}."));

            string script = await File.ReadAllTextAsync(scriptPath, ct);

            // Templates sit in a folder named after the file, beside it.
            string folderPath = string.Empty;
            string? directory = Path.GetDirectoryName(scriptPath);
            if (directory != null)
                folderPath = Path.Combine(directory, Path.GetFileNameWithoutExtension(scriptPath));

            return await ImportTextAsync(script, folderPath, ct);
        }

        public async Task<FlowImportResultDto> ImportTextAsync(string script, string? templateFolderPath, CancellationToken ct = default)
        {
            // Script -> ScriptLines -> LineTokens -> Parse LineTokens -> Schema
            FlowScriptSchema schema = _scanner.Read(script);
            if (!schema.IsValid)
                return Failed(schema.Diagnostics);

            FlowImportResultDto result = new FlowImportResultDto
            {
                IsSuccess = true,
                FlowName = schema.Flow.Name,
                StepCount = schema.Steps.Count,
            };

            foreach (FlowStepTemplate template in schema.Steps.SelectMany(x => x.FlowStepTemplates))
                await ReadTemplateAsync(template, schema, templateFolderPath, result, ct);

            // A sub-flow is another file and is not resolved yet (FLOW-FORMAT.md).
            ResultDto<(int FlowId, FlowValidationResultDto Validation)> replaced = await _dataService.Flow.ReplaceAsync(schema, ct);

            result.FlowId = replaced.Data.FlowId;
            result.Validation = replaced.Data.Validation;

            return result;
        }


        // ================================================================
        // Private methods
        // ================================================================

        // What only the png knows: its bytes, and where its middle is for a click the header left
        // out. The script names a template by its file, and the row by the file without its extension.
        private static async Task ReadTemplateAsync(FlowStepTemplate template, FlowScriptSchema schema, string? templateFolderPath, FlowImportResultDto result, CancellationToken ct)
        {
            string fileName = template.Name;

            // A missing image is reported rather than fatal: the step is still the step, and a
            // flow whose pictures did not come over is more use than no flow at all.
            byte[]? image = null;
            if (templateFolderPath != null)
            {
                string path = Path.Combine(templateFolderPath, fileName);
                if (File.Exists(path))
                    image = await File.ReadAllBytesAsync(path, ct);
            }

            if (image == null)
                result.MissingTemplates.Add(fileName);

            if (schema.TemplatesWithoutClick.Contains(fileName))
            {
                Point click = Centre(image);
                template.ClickOffsetX = click.X;
                template.ClickOffsetY = click.Y;
            }

            template.Name = Path.GetFileNameWithoutExtension(fileName);
            template.TemplateImage = image;

            result.TemplateCount++;
        }

        // Where the capture form puts a new template's click. A png says its size in the IHDR chunk
        // at a fixed offset: eight bytes of signature, four of length and four of "IHDR", then the
        // width and height as big-endian 32-bit numbers. Anything else clicks the top left.
        private static Point Centre(byte[]? image)
        {
            if (image == null || image.Length < 24 || !image.AsSpan(0, 8).SequenceEqual(PngSignature))
                return Point.Empty;

            int width = BinaryPrimitives.ReadInt32BigEndian(image.AsSpan(16, 4));
            int height = BinaryPrimitives.ReadInt32BigEndian(image.AsSpan(20, 4));

            // Rounding half up, as the form's Math.round does.
            return new Point((width + 1) / 2, (height + 1) / 2);
        }

        private static FlowImportResultDto Failed(params Diagnostic[] errors)
        {
            return Failed((IReadOnlyList<Diagnostic>)errors);
        }

        private static FlowImportResultDto Failed(IReadOnlyList<Diagnostic> errors)
        {
            return new FlowImportResultDto
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
