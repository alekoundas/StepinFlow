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

        /// <summary>
        /// Save to Disk the flowscript along with a folder with png immages from templates.
        /// Png name used is the FlowStepTemplate name
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

            // Their pixels are megabytes, so only when asked for. Not tracked, so linked to their steps here.
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
                    TemplateImage = includeImages ? x.TemplateImage : null,
                })
                .ToListAsync(ct);

            foreach (FlowStep step in schema.Steps)
                step.FlowStepTemplates = templates.Where(x => x.FlowStepId == step.Id).ToList();

            await AddSubFlowPathsAsync(dbContext, schema, ct);

            return schema;
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
                .ToDictionaryAsync(x => x.Id, x => FileNameHelper.Clean(x.Name, "sub-flow") + ".sflw", ct);

            foreach (FlowStep step in schema.Steps.Where(x => x.SubFlowId != null))
            {
                if (paths.TryGetValue(step.SubFlowId!.Value, out string? path))
                    schema.SubFlowPaths[step] = path;
            }
        }

        // Write all PNGs to the disk.
        private static async Task<int> WriteTemplatesAsync(FlowScriptSchema schema, string templateFolder, CancellationToken ct)
        {
            // Gather templates.
            List<FlowStepTemplate> templates = schema.Steps
                .SelectMany(x => x.FlowStepTemplates)
                .Where(x => x.TemplateImage != null)
                .ToList();

            if (templates.Count == 0)
                return 0;

            // Create template folder in disk.
            Directory.CreateDirectory(templateFolder);

            // Write to disk.
            foreach (FlowStepTemplate template in templates)
                await File.WriteAllBytesAsync(Path.Combine(templateFolder, template.Name), template.TemplateImage!, ct);

            return templates.Count;
        }
    }
}
