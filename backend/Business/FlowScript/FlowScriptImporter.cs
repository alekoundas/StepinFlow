using System.Buffers.Binary;
using System.Drawing;

using Core.Models.Database;
using Core.Models.Dtos;

using DataAccess;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Business.FlowScript.Diagnostics;
using Business.FlowScript.Models;
using Business.FlowScript.Scanner;

namespace Business.FlowScript
{
    /// <summary>
    /// Import .sflw file into the database. The mirror of FlowScriptExporter.cs.
    /// Parse, then replace the flow's rows with the ones the file reads into.
    /// DOESNT delete the old Flow, just its rows: the Flow stays for its execution history.
    /// </summary>
    public sealed class FlowScriptImporter : IFlowScriptImporter
    {
        private static readonly byte[] PngSignature = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];

        private readonly IDbContextFactory<AppDbContext> _dbContextFactory;
        private readonly IScanner _scanner;

        public FlowScriptImporter(IDbContextFactory<AppDbContext> dbContextFactory, IScanner reader)
        {
            _dbContextFactory = dbContextFactory;
            _scanner = reader;
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

            await using AppDbContext dbContext = await _dbContextFactory.CreateDbContextAsync(ct);

            // Find existing Flow (if any), so all relation rows are droped. Only Flow stays for Execution history.
            Flow? existing = await dbContext.Flows.FirstOrDefaultAsync(x => x.PublicId == schema.Flow.PublicId, ct);

            // Begin DB Transaction.
            await using IDbContextTransaction transaction = await dbContext.Database.BeginTransactionAsync(ct);

            try
            {
                // Saved on its own first: RootId names the flow by id, with no link EF could fill it from.
                Flow flow = existing ?? schema.Flow;
                flow.Name = schema.Flow.Name;

                if (existing == null)
                    dbContext.Flows.Add(flow);

                await dbContext.SaveChangesAsync(ct);

                // Delete relations.
                if (existing != null)
                {
                    await dbContext.FlowSteps.Where(x => x.RootId == flow.Id).ExecuteDeleteAsync(ct);
                    await dbContext.FlowPoints.Where(x => x.FlowId == flow.Id).ExecuteDeleteAsync(ct);
                    await dbContext.FlowAreas.Where(x => x.FlowId == flow.Id).ExecuteDeleteAsync(ct);
                    await dbContext.FlowCsvColumns.Where(x => x.FlowId == flow.Id).ExecuteDeleteAsync(ct);
                    await dbContext.FlowViewports.Where(x => x.FlowId == flow.Id).ExecuteDeleteAsync(ct);
                }

                FlowImportResultDto result = await AddAsync(dbContext, flow, schema, templateFolderPath, ct);

                // One save for the whole graph: EF inserts in dependency order and fills every key
                // from the links.
                await dbContext.SaveChangesAsync(ct);
                await transaction.CommitAsync(ct);

                return result;
            }
            catch (Exception)
            {
                await transaction.RollbackAsync(ct);
                throw;
            }
        }


        // ================================================================
        // Private methods
        // ================================================================

        private static async Task<FlowImportResultDto> AddAsync(AppDbContext dbContext, Flow flow, FlowScriptSchema schema, string? templateFolderPath, CancellationToken ct)
        {
            FlowImportResultDto result = new FlowImportResultDto
            {
                IsSuccess = true,
                FlowId = flow.Id,
                FlowName = flow.Name,
                StepCount = schema.Steps.Count,
            };

            foreach (FlowArea area in schema.Areas)
                area.FlowId = flow.Id;

            foreach (FlowPoint point in schema.Points)
                point.FlowId = flow.Id;

            foreach (FlowCsvColumn input in schema.Inputs)
                input.FlowId = flow.Id;

            foreach (FlowViewport viewport in schema.Viewports)
                viewport.FlowId = flow.Id;

            // A step is either a root step of the Flow or the child of another step, never both.
            // A sub-flow is another file and is not resolved yet (FLOW-FORMAT.md).
            foreach (FlowStep step in schema.Steps)
            {
                step.RootId = flow.Id;
                if (step.ParentFlowStep == null)
                    step.FlowId = flow.Id;

                foreach (FlowStepTemplate template in step.FlowStepTemplates)
                    await ReadTemplateAsync(template, schema, templateFolderPath, result, ct);
            }

            dbContext.FlowAreas.AddRange(schema.Areas);
            dbContext.FlowPoints.AddRange(schema.Points);
            dbContext.FlowCsvColumns.AddRange(schema.Inputs);
            dbContext.FlowViewports.AddRange(schema.Viewports);
            dbContext.FlowSteps.AddRange(schema.Steps);

            return result;
        }

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
