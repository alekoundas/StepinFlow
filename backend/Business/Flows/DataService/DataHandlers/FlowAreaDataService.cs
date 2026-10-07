using Business.Validation;
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
    }
}
