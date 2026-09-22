using Core.Helpers;
using Core.Models.Database;
using Core.Models.Dtos;
using DataAccess;
using Microsoft.EntityFrameworkCore;
using Business.FlowScript.Binding;
using Business.FlowScript.Text;

namespace Business.FlowScript
{
    /// <summary>
    /// Loads a flow out of the database and hands it to the writer.
    ///
    /// The writer is a pure function over <see cref="BoundFlow"/>; this is the part that
    /// knows about EF, files and paths, which is why they are separate classes. Everything is
    /// loaded in one pass and ordered here, because the writer's determinism is only worth anything
    /// if what reaches it is deterministic too.
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

            BoundFlow source = await BuildSourceAsync(dbContext, flowId, ct);

            return _writer.Write(source);
        }

        public async Task<FlowExportResultDto> ExportAsync(int flowId, string? folderPath = null, CancellationToken ct = default)
        {
            await using AppDbContext dbContext = await _dbContextFactory.CreateDbContextAsync(ct);

            BoundFlow source = await BuildSourceAsync(dbContext, flowId, ct);
            string script = _writer.Write(source);

            string folder = string.IsNullOrWhiteSpace(folderPath) ? PathHelper.GetExportDataPath() : folderPath;
            Directory.CreateDirectory(folder);

            string flowFileName = FileNameOf(source.Flow.Name, "flow");
            string scriptPath = Path.Combine(folder, flowFileName + ".sflw");

            // Templates go in a folder named after the flow, beside the script, so git can show
            // which image changed rather than that an archive did.
            string templateFolder = Path.Combine(folder, flowFileName);
            int written = await WriteTemplatesAsync(dbContext, flowId, source, templateFolder, ct);

            await File.WriteAllTextAsync(scriptPath, script.ReplaceLineEndings("\n"), ct);

            return new FlowExportResultDto
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

        // Load EVERYTHING!
        private static async Task<BoundFlow> BuildSourceAsync(AppDbContext dbContext, int flowId, CancellationToken ct)
        {
            Flow flow = await dbContext.Flows.AsNoTracking().FirstOrDefaultAsync(x => x.Id == flowId, ct) 
                ?? throw new InvalidOperationException($"There is no flow {flowId} to export.");

            List<FlowArea> areas = await dbContext.FlowAreas.AsNoTracking()
                .Where(x => x.FlowId == flowId).ToListAsync(ct);

            List<FlowPoint> points = await dbContext.FlowPoints.AsNoTracking()
                .Where(x => x.FlowId == flowId).ToListAsync(ct);

            List<FlowCsvColumn> inputs = await dbContext.FlowCsvColumns.AsNoTracking()
                .Where(x => x.FlowId == flowId).ToListAsync(ct);

            List<FlowViewport> viewports = await dbContext.FlowViewports.AsNoTracking()
                .Where(x => x.FlowId == flowId).ToListAsync(ct);

            // RootId rather than FlowId: it is denormalised onto every descendant, so the whole
            // tree arrives in one query instead of a recursive walk.
            List<FlowStep> steps = await dbContext.FlowSteps.AsNoTracking()
                .Where(x => x.RootId == flowId).ToListAsync(ct);

            // Names and order only. The images are megabytes and nothing here needs the pixels.
            var templates = await dbContext.FlowStepTemplates.AsNoTracking()
                .Where(x => x.FlowStep.RootId == flowId)
                .OrderBy(x => x.FlowStepId).ThenBy(x => x.OrderNumber).ThenBy(x => x.Id)
                .Select(x => new { x.Id, x.FlowStepId, x.Name })
                .ToListAsync(ct);

            Dictionary<int, string> stepNames = steps.ToDictionary(x => x.Id, x => x.Name);

            // A sub-flow step names a file the parser has to find, and it sits beside this one.
            List<int> subFlowIds = steps
                .Where(x => x.SubFlowId != null)
                .Select(x => x.SubFlowId!.Value)
                .Distinct()
                .ToList();

            Dictionary<int, string> subFlowPaths = await dbContext.Flows.AsNoTracking()
                .Where(x => subFlowIds.Contains(x.Id))
                .Select(x => new { x.Id, x.Name })
                .ToDictionaryAsync(x => x.Id, x => FileNameOf(x.Name, "sub-flow") + ".sflw", ct);

            return new BoundFlow
            {
                Flow = flow,
                Areas = areas,
                Points = points,
                Inputs = inputs,
                Viewports = viewports,
                Steps = steps,
                AreaNamesById = areas.ToDictionary(x => x.Id, x => x.Name),
                PointNamesById = points.ToDictionary(x => x.Id, x => x.Name),
                StepNamesById = stepNames,
                SubFlowPathsById = subFlowPaths,
                TemplateFileNamesByStepId = TemplateFileNames(templates.Select(x => (x.Id, x.FlowStepId, x.Name)).ToList(), stepNames),
            };
        }


        /// <summary>
        /// One file name per template, unique within the flow.
        ///
        /// Named after the template rather than by content hash. A hash would dedupe identical
        /// images, but it also changes whenever the image is edited, so git would record a delete
        /// and an add instead of a modification - losing the one thing putting templates in a
        /// repository is for.
        /// </summary>
        private static Dictionary<int, IReadOnlyList<string>> TemplateFileNames(IReadOnlyList<(int Id, int FlowStepId, string Name)> templates, IReadOnlyDictionary<int, string> stepNames)
        {
            Dictionary<int, List<string>> byStep = new Dictionary<int, List<string>>();
            HashSet<string> taken = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach ((int _, int flowStepId, string name) in templates)
            {
                string desired = string.IsNullOrWhiteSpace(name)
                    ? stepNames.GetValueOrDefault(flowStepId, "template")
                    : name;

                string fileName = FlowNameHelper.MakeUnique(FileNameOf(desired, "template"), taken);
                taken.Add(fileName);

                if (!byStep.TryGetValue(flowStepId, out List<string>? names))
                {
                    names = new List<string>();
                    byStep[flowStepId] = names;
                }

                names.Add(fileName + ".png");
            }

            return byStep.ToDictionary(x => x.Key, x => (IReadOnlyList<string>)x.Value);
        }

        private static async Task<int> WriteTemplatesAsync(AppDbContext dbContext, int flowId, BoundFlow source, string templateFolder, CancellationToken ct)
        {
            var images = await dbContext.FlowStepTemplates.AsNoTracking()
                .Where(x => x.FlowStep.RootId == flowId && x.TemplateImage != null)
                .OrderBy(x => x.FlowStepId).ThenBy(x => x.OrderNumber).ThenBy(x => x.Id)
                .Select(x => new { x.FlowStepId, x.TemplateImage })
                .ToListAsync(ct);

            if (images.Count == 0)
                return 0;

            Directory.CreateDirectory(templateFolder);

            int written = 0;
            Dictionary<int, int> nextIndex = new Dictionary<int, int>();

            foreach (var image in images)
            {
                IReadOnlyList<string> names = source.TemplateFileNamesByStepId.GetValueOrDefault(image.FlowStepId, []);

                int index = nextIndex.GetValueOrDefault(image.FlowStepId);
                nextIndex[image.FlowStepId] = index + 1;

                if (index >= names.Count)
                    continue;

                await File.WriteAllBytesAsync(Path.Combine(templateFolder, names[index]), image.TemplateImage!, ct);
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

            char[] invalid = Path.GetInvalidFileNameChars();
            IEnumerable<char> mapped = trimmed.Select(x => invalid.Contains(x) || char.IsWhiteSpace(x) ? '-' : x);

            string cleaned = string.Join('-', new string(mapped.ToArray())
                .Split('-', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                .Trim('.', '-');

            return cleaned.Length == 0 ? fallback : cleaned;
        }
    }
}
