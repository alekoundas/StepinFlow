using System.Text;

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
    /// Exporting a flow means a new flowscript file named after the flow name + .sflw is added to disk.
    /// Also all FlowStepTemplates are added in the folder named after the flow and contains all the 
    /// png images named after the FlowStepTemplate name.
    /// </summary>
    public sealed class FlowScriptExporter : IFlowScriptExporter
    {
        private readonly IDbContextFactory<AppDbContext> _dbContextFactory;
        private readonly IPrinter _printer;

        public FlowScriptExporter(IDbContextFactory<AppDbContext> dbContextFactory, IPrinter printer)
        {
            _dbContextFactory = dbContextFactory;
            _printer = printer;
        }


        // ================================================================
        // Public methods
        // ================================================================


        /// <summary>
        /// Get the script text only. Nothing toches the disk.
        /// </summary>
        public async Task<string> RenderAsync(int flowId, CancellationToken ct = default)
        {
            await using AppDbContext dbContext = await _dbContextFactory.CreateDbContextAsync(ct);

            FlowScriptSchema schema = await LoadAsync(dbContext, flowId, includeImages: false, ct);
            string script = _printer.Write(schema);

            return script;
        }

        // TODO: Remove
        public async Task<FlowScriptSchema> LoadAsync(int flowId, CancellationToken ct = default)
        {
            await using AppDbContext dbContext = await _dbContextFactory.CreateDbContextAsync(ct);
            return await LoadAsync(dbContext, flowId, includeImages: true, ct);
        }

        /// <summary>
        /// Save to Disk the flowscript along with a folder with png immages from templates.
        /// Over an earlier export it writes only what changed, and removes the images the flow no longer uses.
        /// </summary>
        public async Task<FlowScriptExportResultDto> ExportAsync(int flowId, string? folderPath = null, CancellationToken ct = default)
        {
            await using AppDbContext dbContext = await _dbContextFactory.CreateDbContextAsync(ct);

            // Load data.
            FlowScriptSchema schema = await LoadAsync(dbContext, flowId, includeImages: true, ct);

            // Generate script.
            string script = _printer.Write(schema);

            // Create export folder to disk.
            string folder;
            if (string.IsNullOrWhiteSpace(folderPath))
                folder = PathHelper.GetExportDataPath();
            else
                folder = folderPath;
            Directory.CreateDirectory(folder);


            string flowFileName = FileNameHelper.Clean(schema.Flow.Name, "flow");
            string scriptPath = Path.Combine(folder, flowFileName + ".sflw");
            string templateFolder = Path.Combine(folder, flowFileName); // Templates go in a folder named after the flow, beside the script, so git can show which image changed.

            int written = await WriteTemplatesAsync(schema, templateFolder, ct);

            byte[] scriptBytes = Encoding.UTF8.GetBytes(script.ReplaceLineEndings("\n"));
            await WriteIfChangedAsync(scriptPath, scriptBytes, ct);

            return new FlowScriptExportResultDto
            {
                ScriptPath = scriptPath,
                TemplateFolderPath = written > 0 ? templateFolder : string.Empty,
                TemplateCount = written,
                Script = script,
            };
        }


        public List<FlowScriptFileDto> ListScripts(string folderPath)
        {
            if (!Directory.Exists(folderPath))
                return [];

            return Directory.EnumerateFiles(folderPath, "*.sflw")
                .Select(x => new FlowScriptFileDto
                {
                    Path = x,
                    FileName = Path.GetFileName(x),
                    ModifiedAt = File.GetLastWriteTime(x),
                    TemplateCount = TemplateCountOf(x),
                })
                .OrderByDescending(x => x.ModifiedAt)
                .ToList();
        }


        // ================================================================
        // Private methods
        // ================================================================

        // The folder named after the script, beside it, as an export writes it.
        private static int TemplateCountOf(string scriptPath)
        {
            string directory = Path.GetDirectoryName(scriptPath) ?? string.Empty;
            string name = Path.GetFileNameWithoutExtension(scriptPath);
            string templateFolder = Path.Combine(directory, name);

            if (!Directory.Exists(templateFolder))
                return 0;

            return Directory.EnumerateFiles(templateFolder).Count(FlowScriptFile.IsPngName);
        }

        private static async Task<FlowScriptSchema> LoadAsync(AppDbContext dbContext, int flowId, bool includeImages, CancellationToken ct)
        {
            Flow flow = await dbContext.Flows.FirstOrDefaultAsync(x => x.Id == flowId, ct)
                ?? throw new InvalidOperationException($"There is no flow {flowId} to export.");

            FlowScriptSchema schema = new FlowScriptSchema() { Flow = flow };
            schema.Areas.AddRange(await dbContext.FlowAreas.Where(x => x.FlowId == flowId).ToListAsync(ct));
            schema.Points.AddRange(await dbContext.FlowPoints.Where(x => x.FlowId == flowId).ToListAsync(ct));
            schema.Inputs.AddRange(await dbContext.FlowCsvColumns.Where(x => x.FlowId == flowId).ToListAsync(ct));
            schema.Viewports.AddRange(await dbContext.FlowViewports.Where(x => x.FlowId == flowId).ToListAsync(ct));
            schema.Steps.AddRange(await dbContext.FlowSteps.Where(x => x.RootId == flowId).ToListAsync(ct));

            // Their pixels are megabytes, so only when asked for. Not tracked, so linked to their steps
            // here, each picture once however many steps search for it.
            List<FlowTemplate> pictures = await dbContext.FlowTemplates.AsNoTracking()
                .Where(x => x.FlowStepTemplates.Any(y => y.FlowStep.RootId == flowId))
                .Select(x => new FlowTemplate
                {
                    Id = x.Id,
                    Name = x.Name,
                    ClickOffsetX = x.ClickOffsetX,
                    ClickOffsetY = x.ClickOffsetY,
                    AuthoredFlowAreaWidth = x.AuthoredFlowAreaWidth,
                    AuthoredFlowAreaHeight = x.AuthoredFlowAreaHeight,
                    AuthoredDpi = x.AuthoredDpi,
                    TemplateImage = includeImages ? x.TemplateImage : null,
                })
                .ToListAsync(ct);

            Dictionary<int, FlowTemplate> pictureById = pictures.ToDictionary(x => x.Id);

            List<FlowStepTemplate> templates = await dbContext.FlowStepTemplates.AsNoTracking()
                .Where(x => x.FlowStep.RootId == flowId)
                .OrderBy(x => x.FlowStepId).ThenBy(x => x.OrderNumber).ThenBy(x => x.Id)
                .Select(x => new FlowStepTemplate
                {
                    Id = x.Id,
                    FlowStepId = x.FlowStepId,
                    FlowTemplateId = x.FlowTemplateId,
                    OrderNumber = x.OrderNumber,
                    Accuracy = x.Accuracy,
                    IsRequired = x.IsRequired,
                })
                .ToListAsync(ct);

            foreach (FlowStepTemplate template in templates)
                template.FlowTemplate = pictureById[template.FlowTemplateId];

            foreach (FlowStep step in schema.Steps)
                step.FlowStepTemplates = templates.Where(x => x.FlowStepId == step.Id).ToList();

            await AddSubFlowPathsAsync(dbContext, schema, ct);

            return schema;
        }

        // The file a sub-flow exports to, which is how a step that executes it names it.
        internal static string SubFlowFileName(string flowName)
        {
            return FileNameHelper.Clean(flowName, "sub-flow") + ".sflw";
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
                .ToDictionaryAsync(x => x.Id, x => SubFlowFileName(x.Name), ct);

            foreach (FlowStep step in schema.Steps.Where(x => x.SubFlowId != null))
            {
                if (paths.TryGetValue(step.SubFlowId!.Value, out string? path))
                    schema.SubFlowPaths[step] = path;
            }
        }

        // Write the PNGs whose bytes changed, and remove the ones the flow no longer names, so an
        // export over an earlier one brings it up to date.
        private static async Task<int> WriteTemplatesAsync(FlowScriptSchema schema, string templateFolder, CancellationToken ct)
        {
            // Gather templates.
            List<FlowTemplate> templates = schema.Steps
                .SelectMany(x => x.FlowStepTemplates)
                .Select(x => x.FlowTemplate)
                .Distinct()
                .ToList();

            List<FlowTemplate> pictures = templates.Where(x => x.TemplateImage != null).ToList();

            // Create template folder in disk.
            if (pictures.Count > 0)
                Directory.CreateDirectory(templateFolder);

            // Write to disk.
            foreach (FlowTemplate picture in pictures)
                await WriteIfChangedAsync(Path.Combine(templateFolder, picture.Name), picture.TemplateImage!, ct);

            // Every name the flow has, not only the images written: a template imported with its
            // image missing has no bytes, and a file put back by hand is then the only copy.
            HashSet<string> names = templates.Select(x => x.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
            RemoveUnusedImages(templateFolder, names);

            return pictures.Count;
        }

        // Top level only, and only PNGs, so whatever else someone keeps in the folder survives.
        private static void RemoveUnusedImages(string templateFolder, IReadOnlySet<string> names)
        {
            if (!Directory.Exists(templateFolder))
                return;

            List<string> paths = Directory.EnumerateFiles(templateFolder, "*", SearchOption.TopDirectoryOnly).ToList();

            foreach (string path in paths)
            {
                string name = Path.GetFileName(path);
                if (FlowScriptFile.IsPngName(name) && !names.Contains(name))
                    File.Delete(path);
            }

            if (!Directory.EnumerateFileSystemEntries(templateFolder).Any())
                Directory.Delete(templateFolder);
        }

        // Only when the bytes differ, so a file that has not changed keeps its time and git never looks at it.
        private static async Task WriteIfChangedAsync(string path, byte[] bytes, CancellationToken ct)
        {
            if (File.Exists(path))
            {
                byte[] current = await File.ReadAllBytesAsync(path, ct);
                if (current.AsSpan().SequenceEqual(bytes))
                    return;
            }

            await File.WriteAllBytesAsync(path, bytes, ct);
        }
    }
}
