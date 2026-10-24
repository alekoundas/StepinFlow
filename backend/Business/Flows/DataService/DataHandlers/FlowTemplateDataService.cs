using Business.Flows.FlowValidationService;
using Core.Helpers;
using Core.Models.Database;
using Core.Models.Dtos;
using DataAccess;
using Microsoft.EntityFrameworkCore;

namespace Business.Flows.DataService.DataHandlers
{
    /// <summary>
    /// Every change to a flow's template images. A template is created by the step that captures
    /// it, and goes when the last step stops using it.
    /// </summary>
    public sealed class FlowTemplateDataService : BaseDataService
    {
        public FlowTemplateDataService(IDbContextFactory<AppDbContext> dbContextFactory, IFlowValidationService flowValidationService)
            : base(dbContextFactory, flowValidationService)
        {
        }


        // ================================================================
        // Public methods
        // ================================================================

        /// <summary>
        /// The picture and its click point, for every step that links to it. The name stays: it is
        /// the file name.
        /// </summary>
        public async Task<ResultDto<FlowTemplate>> UpdateAsync(FlowTemplateDto dto, CancellationToken ct)
        {
            await using AppDbContext dbContext = await DbContextFactory.CreateDbContextAsync(ct);

            FlowTemplate? template = await dbContext.FlowTemplates.FirstOrDefaultAsync(x => x.Id == dto.Id, ct);
            if (template == null)
                return ResultDto<FlowTemplate>.Failure("Entity doesnt exist in the Database!");

            CopyPicture(dto, template);
            await dbContext.SaveChangesAsync(ct);

            return ResultDto<FlowTemplate>.Success(template);
        }

        /// <summary>
        /// What the form changed about a picture. The blob is only overwritten when one was sent:
        /// a list sends templates back without their bytes so a save does not push megabytes.
        /// </summary>
        public static void CopyPicture(FlowTemplateDto dto, FlowTemplate template)
        {
            if (dto.TemplateImage != null && dto.TemplateImage.Length > 0)
                template.TemplateImage = dto.TemplateImage;

            template.ClickOffsetX = dto.ClickOffsetX;
            template.ClickOffsetY = dto.ClickOffsetY;

            template.AuthoredFlowAreaWidth = dto.AuthoredFlowAreaWidth;
            template.AuthoredFlowAreaHeight = dto.AuthoredFlowAreaHeight;
            template.AuthoredDpi = dto.AuthoredDpi;
        }

        /// <summary>
        /// A template's name is its file name, and a flow keeps its templates in one folder, so
        /// names are compared ignoring case as Windows and macOS do.
        /// </summary>
        public static async Task<HashSet<string>> TakenFileNamesAsync(AppDbContext dbContext, int flowId, CancellationToken ct)
        {
            List<string> names = await dbContext.FlowTemplates
                .AsNoTracking()
                .Where(x => x.FlowId == flowId)
                .Select(x => x.Name)
                .ToListAsync(ct);

            return new HashSet<string>(names, StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>Nobody names a template: a new one draws a file name the flow does not have.</summary>
        public static string NewName(HashSet<string> taken)
        {
            string name = FileNameHelper.GenerateTemplateFileName();
            while (!taken.Add(name))
                name = FileNameHelper.GenerateTemplateFileName();

            return name;
        }

        /// <summary>
        /// The flow's templates no step links to any more, so the flow, its script and its folder
        /// hold the same pictures.
        /// </summary>
        public static Task<int> RemoveUnusedAsync(AppDbContext dbContext, int flowId, CancellationToken ct)
        {
            return dbContext.FlowTemplates
                .Where(x => x.FlowId == flowId && !x.FlowStepTemplates.Any())
                .ExecuteDeleteAsync(ct);
        }
    }
}
