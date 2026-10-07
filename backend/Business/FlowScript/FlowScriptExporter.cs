using Core.Helpers;
using Core.Models.Database;
using Core.Models.Dtos.FlowScript;
using DataAccess;
using Microsoft.EntityFrameworkCore;
using Business.FlowScript.Models;
using Business.FlowScript.Text;

namespace Business.FlowScript
{
    /// <summary>
    /// Loads a flow out of the database and hands it to the printer.
    ///
    /// The printer is a pure function over <see cref="FlowScriptSchema"/>; this is the part that
    /// knows about EF, files and paths, which is why they are separate classes.
    /// </summary>
    public sealed class FlowScriptExporter : IFlowScriptExporter
    {
        private readonly IDbContextFactory<AppDbContext> _dbContextFactory;
        private readonly IPrinter _writer;

        public FlowScriptExporter(IDbContextFactory<AppDbContext> dbContextFactory, IPrinter writer)
        {
            _dbContextFactory = dbContextFactory;
            _writer = writer;
        }


        // ================================================================
        // Public methods
        // ================================================================

        public async Task<string> RenderAsync(int flowId, CancellationToken ct = default)
        {
            await using AppDbContext dbContext = await _dbContextFactory.CreateDbContextAsync(ct);

            FlowScriptSchema schema = await LoadAsync(dbContext, flowId, ct);

            return _writer.Write(schema);
        }

        public async Task<FlowScriptExportResultDto> ExportAsync(int flowId, string? folderPath = null, CancellationToken ct = default)
        {
            await using AppDbContext dbContext = await _dbContextFactory.CreateDbContextAsync(ct);

            FlowScriptSchema schema = await LoadAsync(dbContext, flowId, ct);
            string script = _writer.Write(schema);

            string folder = string.IsNullOrWhiteSpace(folderPath) ? PathHelper.GetExportDataPath() : folderPath;
            Directory.CreateDirectory(folder);

            string flowFileName = FileNameOf(schema.Flow.Name, "flow");
            string scriptPath = Path.Combine(folder, flowFileName + ".sflw");

            // Templates go in a folder named after the flow, beside the script, so git can show
            // which image changed rather than that an archive did.
            string templateFolder = Path.Combine(folder, flowFileName);
            int written = await WriteTemplatesAsync(dbContext, schema, templateFolder, ct);

            await File.WriteAllTextAsync(scriptPath, script.ReplaceLineEndings("\n"), ct);

            return new FlowScriptExportResultDto
            {
                ScriptPath = scriptPath,
                TemplateFolderPath = written > 0 ? templateFolder : string.Empty,
                TemplateCount = written,
                Script = script,
            };
        }


        // ================================================================
        // Private methods
        // ================================================================

        // Every row of the flow into one context, tracked: EF links each one to the rows it names
        // as they load, so what comes back already is the schema the printer reads.
        private static async Task<FlowScriptSchema> LoadAsync(AppDbContext dbContext, int flowId, CancellationToken ct)
        {
            Flow flow = await dbContext.Flows.FirstOrDefaultAsync(x => x.Id == flowId, ct)
                ?? throw new InvalidOperationException($"There is no flow {flowId} to export.");

            FlowScriptSchema schema = new FlowScriptSchema() { Flow = flow };
            schema.Areas.AddRange(await dbContext.FlowAreas.Where(x => x.FlowId == flowId).ToListAsync(ct));
            schema.Points.AddRange(await dbContext.FlowPoints.Where(x => x.FlowId == flowId).ToListAsync(ct));
            schema.Inputs.AddRange(await dbContext.FlowCsvColumns.Where(x => x.FlowId == flowId).ToListAsync(ct));
            schema.Viewports.AddRange(await dbContext.FlowViewports.Where(x => x.FlowId == flowId).ToListAsync(ct));
            schema.Steps.AddRange(await dbContext.FlowSteps.Where(x => x.RootId == flowId).ToListAsync(ct));

            // Without their pixels, which are megabytes. Not tracked, so linked to their steps here.
            List<FlowStepTemplate> templates = await dbContext.FlowStepTemplates.AsNoTracking()
                .Where(x => x.FlowStep.RootId == flowId)
                .OrderBy(x => x.FlowStepId).ThenBy(x => x.OrderNumber).ThenBy(x => x.Id)
                .Select(x => new FlowStepTemplate
                {
                    Id = x.Id,
                    FlowStepId = x.FlowStepId,
                    Name = x.Name,
                    OrderNumber = x.OrderNumber,
                    Accuracy = x.Accuracy,
                    IsRequired = x.IsRequired,
                    ClickOffsetX = x.ClickOffsetX,
                    ClickOffsetY = x.ClickOffsetY,
                    AuthoredFlowAreaWidth = x.AuthoredFlowAreaWidth,
                    AuthoredFlowAreaHeight = x.AuthoredFlowAreaHeight,
                    AuthoredDpi = x.AuthoredDpi,
                })
                .ToListAsync(ct);

            foreach (FlowStep step in schema.Steps)
                step.FlowStepTemplates = templates.Where(x => x.FlowStepId == step.Id).ToList();

            NameTemplateFiles(schema, templates);
            await AddSubFlowPathsAsync(dbContext, schema, ct);

            return schema;
        }

        /// <summary>
        /// One file name per template, unique within the flow, written over the template's name -
        /// in a script a template is named by its file.
        ///
        /// Named after the template rather than by content hash. A hash would dedupe identical
        /// images, but it also changes whenever the image is edited, so git would record a delete
        /// and an add instead of a modification - losing the one thing putting templates in a
        /// repository is for.
        /// </summary>
        private static void NameTemplateFiles(FlowScriptSchema schema, IReadOnlyList<FlowStepTemplate> templates)
        {
            Dictionary<int, string> stepNames = schema.Steps.ToDictionary(x => x.Id, x => x.Name);
            HashSet<string> taken = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (FlowStepTemplate template in templates)
            {
                string desired = template.Name;
                if (string.IsNullOrWhiteSpace(desired))
                    desired = stepNames.GetValueOrDefault(template.FlowStepId, "template");

                string fileName = FlowNameHelper.MakeUnique(FileNameOf(desired, "template"), taken);
                taken.Add(fileName);

                template.Name = fileName + ".png";
            }
        }

        // A sub-flow step names a file the parser has to find, and it sits beside this one.
        private static async Task AddSubFlowPathsAsync(AppDbContext dbContext, FlowScriptSchema schema, CancellationToken ct)
        {
            List<int> subFlowIds = schema.Steps
                .Where(x => x.SubFlowId != null)
                .Select(x => x.SubFlowId!.Value)
                .Distinct()
                .ToList();

            Dictionary<int, string> paths = await dbContext.Flows.AsNoTracking()
                .Where(x => subFlowIds.Contains(x.Id))
                .Select(x => new { x.Id, x.Name })
                .ToDictionaryAsync(x => x.Id, x => FileNameOf(x.Name, "sub-flow") + ".sflw", ct);

            foreach (FlowStep step in schema.Steps.Where(x => x.SubFlowId != null))
            {
                if (paths.TryGetValue(step.SubFlowId!.Value, out string? path))
                    schema.SubFlowPaths[step] = path;
            }
        }

        private static async Task<int> WriteTemplatesAsync(AppDbContext dbContext, FlowScriptSchema schema, string templateFolder, CancellationToken ct)
        {
            List<FlowStepTemplate> templates = schema.Steps.SelectMany(x => x.FlowStepTemplates).ToList();
            List<int> ids = templates.Select(x => x.Id).ToList();

            Dictionary<int, byte[]> images = await dbContext.FlowStepTemplates.AsNoTracking()
                .Where(x => ids.Contains(x.Id) && x.TemplateImage != null)
                .Select(x => new { x.Id, x.TemplateImage })
                .ToDictionaryAsync(x => x.Id, x => x.TemplateImage!, ct);

            if (images.Count == 0)
                return 0;

            Directory.CreateDirectory(templateFolder);

            int written = 0;
            foreach (FlowStepTemplate template in templates)
            {
                if (!images.TryGetValue(template.Id, out byte[]? image))
                    continue;

                await File.WriteAllBytesAsync(Path.Combine(templateFolder, template.Name), image, ct);
                written++;
            }

            return written;
        }

        // A name a file system will accept, and never empty. Whitespace becomes a hyphen
        private static string FileNameOf(string name, string fallback)
        {
            string trimmed = (name ?? string.Empty).Trim();
            if (trimmed.Length == 0)
                return fallback;

            // < and > on every machine, not only the ones that ban them: a template is named by its
            // file name inside <[ ]>, which cannot hold either quote.
            char[] invalid = Path.GetInvalidFileNameChars().Concat(['<', '>']).ToArray();
            IEnumerable<char> mapped = trimmed.Select(x => invalid.Contains(x) || char.IsWhiteSpace(x) ? '-' : x);

            string cleaned = string.Join('-', new string(mapped.ToArray())
                .Split('-', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                .Trim('.', '-');

            return cleaned.Length == 0 ? fallback : cleaned;
        }
    }
}
