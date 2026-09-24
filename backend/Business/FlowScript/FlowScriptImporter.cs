using Core.Models.Database;
using Core.Models.Dtos;

using DataAccess;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Business.FlowScript.Binding;
using Business.FlowScript.Syntax;
using Business.FlowScript.Diagnostics;

namespace Business.FlowScript
{
    /// <summary>
    /// A .sflw file into the database. The mirror of <see cref="FlowScriptExporter"/>.
    ///
    /// Parse, validate, then replace, and only then: everything before the transaction is pure, so
    /// a file with a typo in it reports the line and leaves the flow exactly as it was. Half a
    /// flow is worse than no import, and a person who hits an error is usually mid-edit.
    ///
    /// Deleting the old steps does not take their history with them: an execution step keeps the
    /// name it ran under and its foreign key is set null rather than cascaded, so the trend for a
    /// step survives a re-import as long as its name does.
    /// </summary>
    public sealed class FlowScriptImporter : IFlowScriptImporter
    {
        private readonly IDbContextFactory<AppDbContext> _dbContextFactory;
        private readonly IParser _reader;

        public FlowScriptImporter(IDbContextFactory<AppDbContext> dbContextFactory, IParser reader)
        {
            _dbContextFactory = dbContextFactory;
            _reader = reader;
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
            string folder = Path.Combine(
                Path.GetDirectoryName(scriptPath) ?? string.Empty,
                Path.GetFileNameWithoutExtension(scriptPath));

            return await ImportTextAsync(script, folder, ct);
        }

        public async Task<FlowImportResultDto> ImportTextAsync(string script, string? templateFolderPath, CancellationToken ct = default)
        {
            FlowSyntax document = _reader.Read(script);
            if (!document.IsValid)
                return Failed(document.Diagnostics);

            List<Diagnostic> errors = new List<Diagnostic>();
            BoundFlow source = Binder.Resolve(document, errors);
            if (errors.Count > 0)
                return Failed(errors);

            await using AppDbContext dbContext = await _dbContextFactory.CreateDbContextAsync(ct);

            Flow? existing = await dbContext.Flows
                .FirstOrDefaultAsync(x => x.PublicId == document.PublicId, ct);

            await using IDbContextTransaction transaction = await dbContext.Database.BeginTransactionAsync(ct);

            try
            {
                Flow flow = existing ?? new Flow { PublicId = document.PublicId };
                flow.Name = document.FlowName;

                if (existing == null)
                    dbContext.Flows.Add(flow);

                await dbContext.SaveChangesAsync(ct);

                if (existing != null)
                    await ClearAsync(dbContext, flow.Id, ct);

                FlowImportResultDto result = await WriteAsync(dbContext, flow, document, source, templateFolderPath, ct);

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

        // The flow's own rows only. Executions are left alone: they are what the file is measured
        // against, and an import is a new version of the test rather than a new test.
        private static async Task ClearAsync(AppDbContext dbContext, int flowId, CancellationToken ct)
        {
            await dbContext.FlowSteps.Where(x => x.RootId == flowId).ExecuteDeleteAsync(ct);
            await dbContext.FlowPoints.Where(x => x.FlowId == flowId).ExecuteDeleteAsync(ct);
            await dbContext.FlowAreas.Where(x => x.FlowId == flowId).ExecuteDeleteAsync(ct);
            await dbContext.FlowCsvColumns.Where(x => x.FlowId == flowId).ExecuteDeleteAsync(ct);
            await dbContext.FlowViewports.Where(x => x.FlowId == flowId).ExecuteDeleteAsync(ct);
        }

        private static async Task<FlowImportResultDto> WriteAsync(
            AppDbContext dbContext,
            Flow flow,
            FlowSyntax document,
            BoundFlow source,
            string? templateFolderPath,
            CancellationToken ct)
        {
            FlowImportResultDto result = new FlowImportResultDto
            {
                IsSuccess = true,
                FlowId = flow.Id,
                FlowName = flow.Name,
            };

            // Areas before points and steps, and roots before children: a parent needs its real id
            // before anything can point at it. The document's ids are positions, not rows.
            Dictionary<int, int> areaIds = await WriteAreasAsync(dbContext, flow, document, ct);
            Dictionary<int, int> pointIds = await WritePointsAsync(dbContext, flow, document, areaIds, ct);

            foreach (FlowViewport viewport in document.Viewports)
            {
                viewport.Id = 0;
                viewport.FlowId = flow.Id;
                dbContext.FlowViewports.Add(viewport);
            }

            foreach (FlowCsvColumn input in document.Inputs)
            {
                input.Id = 0;
                input.FlowId = flow.Id;
                dbContext.FlowCsvColumns.Add(input);
            }

            await WriteStepsAsync(dbContext, flow, document, areaIds, pointIds, templateFolderPath, result, ct);

            result.StepCount = document.Steps.Count;

            return result;
        }

        private static async Task<Dictionary<int, int>> WriteAreasAsync(
            AppDbContext dbContext, Flow flow, FlowSyntax document, CancellationToken ct)
        {
            Dictionary<int, int> ids = new Dictionary<int, int>();

            // Two passes so a child can be given a parent that already has a row.
            foreach (AreaSyntax parsed in document.Areas.OrderBy(x => x.ParentName == null ? 0 : 1))
            {
                int documentId = parsed.Area.Id;
                int? parentDocumentId = parsed.Area.ParentFlowAreaId;

                parsed.Area.Id = 0;
                parsed.Area.FlowId = flow.Id;
                parsed.Area.ParentFlowAreaId = parentDocumentId == null ? null : ids[parentDocumentId.Value];

                dbContext.FlowAreas.Add(parsed.Area);
                await dbContext.SaveChangesAsync(ct);

                ids[documentId] = parsed.Area.Id;
            }

            return ids;
        }

        private static async Task<Dictionary<int, int>> WritePointsAsync(
            AppDbContext dbContext, Flow flow, FlowSyntax document, IReadOnlyDictionary<int, int> areaIds, CancellationToken ct)
        {
            Dictionary<int, int> ids = new Dictionary<int, int>();

            foreach (PointSyntax parsed in document.Points)
            {
                int documentId = parsed.Point.Id;
                int? areaDocumentId = parsed.Point.FlowAreaId;

                parsed.Point.Id = 0;
                parsed.Point.FlowId = flow.Id;
                parsed.Point.FlowAreaId = areaDocumentId == null ? null : areaIds.GetValueOrDefault(areaDocumentId.Value);

                dbContext.FlowPoints.Add(parsed.Point);
                await dbContext.SaveChangesAsync(ct);

                ids[documentId] = parsed.Point.Id;
            }

            return ids;
        }

        private static async Task WriteStepsAsync(
            AppDbContext dbContext,
            Flow flow,
            FlowSyntax document,
            IReadOnlyDictionary<int, int> areaIds,
            IReadOnlyDictionary<int, int> pointIds,
            string? templateFolderPath,
            FlowImportResultDto result,
            CancellationToken ct)
        {
            Dictionary<int, int> stepIds = new Dictionary<int, int>();

            // Parents first, in document order, so a child always has a real parent id. The reader
            // emits a parent before any of its children, which is what makes one pass enough.
            foreach (StepSyntax parsed in document.Steps)
            {
                FlowStep step = parsed.Step;

                int documentId = step.Id;
                int? areaDocumentId = step.FlowAreaId;
                int? pointDocumentId = step.FlowPointId;
                int? pointEndDocumentId = step.FlowPointEndId;
                int? parentDocumentId = step.ParentFlowStepId;

                step.Id = 0;
                step.RootId = flow.Id;
                step.FlowId = parentDocumentId == null ? flow.Id : null;
                step.ParentFlowStepId = parentDocumentId == null ? null : stepIds[parentDocumentId.Value];
                step.FlowAreaId = areaDocumentId == null ? null : areaIds.GetValueOrDefault(areaDocumentId.Value);
                step.FlowPointId = pointDocumentId == null ? null : pointIds.GetValueOrDefault(pointDocumentId.Value);
                step.FlowPointEndId = pointEndDocumentId == null ? null : pointIds.GetValueOrDefault(pointEndDocumentId.Value);

                // A reference can point at a step written below this one, so it is left for the
                // pass after every row exists.
                step.FlowStepReferenceId = null;
                step.FlowStepReferenceEndId = null;
                step.SubFlowId = null;

                dbContext.FlowSteps.Add(step);
                await dbContext.SaveChangesAsync(ct);

                stepIds[documentId] = step.Id;

                result.TemplateCount += await WriteTemplatesAsync(dbContext, step, parsed, templateFolderPath, result, ct);
            }

            ResolveReferences(document, stepIds);
        }

        // The forward references, once every step has a row.
        private static void ResolveReferences(FlowSyntax document, IReadOnlyDictionary<int, int> stepIds)
        {
            Dictionary<string, int> byName = new Dictionary<string, int>(StringComparer.Ordinal);

            foreach (StepSyntax parsed in document.Steps.Where(x => !string.IsNullOrEmpty(x.Step.Name)))
                byName[parsed.Step.Name] = parsed.Step.Id;

            foreach (StepSyntax parsed in document.Steps)
            {
                if (parsed.ReferenceName != null && byName.TryGetValue(parsed.ReferenceName, out int reference))
                    parsed.Step.FlowStepReferenceId = reference;

                if (parsed.ReferenceEndName != null && byName.TryGetValue(parsed.ReferenceEndName, out int referenceEnd))
                    parsed.Step.FlowStepReferenceEndId = referenceEnd;
            }
        }

        private static async Task<int> WriteTemplatesAsync(
            AppDbContext dbContext,
            FlowStep step,
            StepSyntax parsed,
            string? templateFolderPath,
            FlowImportResultDto result,
            CancellationToken ct)
        {
            int written = 0;

            for (int i = 0; i < parsed.Templates.Count; i++)
            {
                string fileName = parsed.Templates[i].FileName;
                string path = templateFolderPath == null ? string.Empty : Path.Combine(templateFolderPath, fileName);

                // A missing image is reported rather than fatal: the step is still the step, and a
                // flow whose pictures did not come over is more use than no flow at all.
                byte[]? image = File.Exists(path) ? await File.ReadAllBytesAsync(path, ct) : null;
                if (image == null)
                    result.MissingTemplates.Add(fileName);

                dbContext.FlowStepTemplates.Add(new FlowStepTemplate
                {
                    FlowStepId = step.Id,
                    Name = Path.GetFileNameWithoutExtension(fileName),
                    OrderNumber = i,
                    TemplateImage = image,
                    IsRequired = true,
                    Accuracy = parsed.Templates[i].Accuracy,
                });

                written++;
            }

            return written;
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
