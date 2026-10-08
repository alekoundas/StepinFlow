using Business.Flows.FlowValidationService;
using Core.Helpers;
using Core.Models.Database;
using Core.Models.Dtos;
using DataAccess;
using Microsoft.EntityFrameworkCore;

namespace Business.Flows.DataService.DataHandlers
{
    /// <summary>
    /// Every change to a flow's areas.
    /// </summary>
    public sealed class FlowAreaDataService : BaseDataService
    {
        public FlowAreaDataService(
            IDbContextFactory<AppDbContext> dbContextFactory,
            IFlowValidationService flowValidationService
            ): base(dbContextFactory, flowValidationService)
        {
        }


        // ================================================================
        // Public methods
        // ================================================================

        public async Task<ResultDto<int>> CreateAsync(FlowArea area, CancellationToken ct)
        {
            await using AppDbContext dbContext = await DbContextFactory.CreateDbContextAsync(ct);

            string? tooDeep = await ValidateDepthAsync(dbContext, 0, area.ParentFlowAreaId, ct);
            if (tooDeep != null)
                return ResultDto<int>.Failure(tooDeep);

            string? notMain = await ValidateMainAsync(dbContext, 0, area.FlowId, area.IsMain, area.ParentFlowAreaId, ct);
            if (notMain != null)
                return ResultDto<int>.Failure(notMain);

            area.Id = 0;
            area.Name = FlowNameHelper.MakeUnique(area.Name, await TakenNamesAsync(dbContext, area.FlowId, ct));

            dbContext.FlowAreas.Add(area);
            await dbContext.SaveChangesAsync(ct);

            return ResultDto<int>.Success(area.Id);
        }

        public async Task<ResultDto<FlowArea>> UpdateAsync(FlowAreaDto dto, CancellationToken ct)
        {
            await using AppDbContext dbContext = await DbContextFactory.CreateDbContextAsync(ct);

            FlowArea? area = await dbContext.FlowAreas.FirstOrDefaultAsync(x => x.Id == dto.Id, ct);
            if (area == null)
                return ResultDto<FlowArea>.Failure("Entity doesnt exist in the Database!");

            string? tooDeep = await ValidateDepthAsync(dbContext, area.Id, dto.ParentFlowAreaId, ct);
            if (tooDeep != null)
                return ResultDto<FlowArea>.Failure(tooDeep);

            string? notMain = await ValidateMainAsync(dbContext, area.Id, area.FlowId, dto.IsMain, dto.ParentFlowAreaId, ct);
            if (notMain != null)
                return ResultDto<FlowArea>.Failure(notMain);

            dbContext.Entry(area).CurrentValues.SetValues(dto);
            await dbContext.SaveChangesAsync(ct);

            return ResultDto<FlowArea>.Success(area);
        }

        public async Task<ResultDto<bool>> DeleteAsync(int id, CancellationToken ct)
        {
            await using AppDbContext dbContext = await DbContextFactory.CreateDbContextAsync(ct);

            int count = await dbContext.FlowAreas.Where(x => x.Id == id).ExecuteDeleteAsync(ct);
            if (count <= 0)
                return ResultDto<bool>.Failure("Entity doesnt exist in the Database!");

            return ResultDto<bool>.Success(true);
        }

        /// <summary>
        /// Areas read from a script, linked to each other. EF inserts a parent before its children
        /// from the links, so the order they come in does not matter.
        /// </summary>
        public static void Add(AppDbContext dbContext, Flow flow, IReadOnlyList<FlowArea> areas)
        {
            foreach (FlowArea area in areas)
                area.FlowId = flow.Id;

            dbContext.FlowAreas.AddRange(areas);
        }


        // ================================================================
        // Private methods
        // ================================================================

        // Areas go one level deep: the parent sits inside nothing, and an area with areas inside it
        // stays a root.
        private static async Task<string?> ValidateDepthAsync(AppDbContext dbContext, int areaId, int? parentId, CancellationToken ct)
        {
            if (parentId == null)
                return null;

            FlowArea? parent = await dbContext.FlowAreas
                .Include(x => x.ParentFlowArea)
                .FirstOrDefaultAsync(x => x.Id == parentId, ct);

            if (parent?.ParentFlowArea != null)
                return $"\"{parent.Name}\" is already inside \"{parent.ParentFlowArea.Name}\". Areas go one level deep.";

            bool hasChildren = await dbContext.FlowAreas.AnyAsync(x => x.ParentFlowAreaId == areaId, ct);
            if (hasChildren)
                return "Other areas sit inside this one, so it cannot go inside another. Areas go one level deep.";

            return null;
        }

        // The main area is the window the flow works in: one of them, and never a region inside another.
        private static async Task<string?> ValidateMainAsync(AppDbContext dbContext, int areaId, int flowId, bool isMain, int? parentId, CancellationToken ct)
        {
            if (!isMain)
                return null;

            if (parentId != null)
                return "An area inside another cannot be the main area. The main area is the window itself.";

            string? other = await dbContext.FlowAreas
                .Where(x => x.FlowId == flowId && x.IsMain && x.Id != areaId)
                .Select(x => x.Name)
                .FirstOrDefaultAsync(ct);

            if (other != null)
                return $"\"{other}\" is already the main area. A flow works in one window.";

            return null;
        }
    }
}
