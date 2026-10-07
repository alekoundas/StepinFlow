using Business.Validation;
using Core.Helpers;
using Core.Models.Database;
using Core.Models.Dtos;
using DataAccess;
using Microsoft.EntityFrameworkCore;

namespace Business.Flows.DataService.DataHandlers
{
    /// <summary>
    /// Every change to a flow's points.
    /// </summary>
    public sealed class FlowPointDataService : BaseDataService
    {
        public FlowPointDataService(IDbContextFactory<AppDbContext> dbContextFactory, IFlowValidationService flowValidationService)
            : base(dbContextFactory, flowValidationService)
        {
        }


        // ================================================================
        // Public methods
        // ================================================================

        public async Task<ResultDto<int>> CreateAsync(FlowPoint point, CancellationToken ct)
        {
            await using AppDbContext dbContext = await DbContextFactory.CreateDbContextAsync(ct);

            point.Id = 0;
            point.Name = FlowNameHelper.MakeUnique(point.Name, await TakenNamesAsync(dbContext, point.FlowId, ct));

            dbContext.FlowPoints.Add(point);
            await dbContext.SaveChangesAsync(ct);

            return ResultDto<int>.Success(point.Id);
        }

        public async Task<ResultDto<FlowPoint>> UpdateAsync(FlowPointDto dto, CancellationToken ct)
        {
            await using AppDbContext dbContext = await DbContextFactory.CreateDbContextAsync(ct);

            FlowPoint? point = await dbContext.FlowPoints.FirstOrDefaultAsync(x => x.Id == dto.Id, ct);
            if (point == null)
                return ResultDto<FlowPoint>.Failure("Entity doesnt exist in the Database!");

            dbContext.Entry(point).CurrentValues.SetValues(dto);
            await dbContext.SaveChangesAsync(ct);

            return ResultDto<FlowPoint>.Success(point);
        }

        public async Task<ResultDto<bool>> DeleteAsync(int id, CancellationToken ct)
        {
            await using AppDbContext dbContext = await DbContextFactory.CreateDbContextAsync(ct);

            int count = await dbContext.FlowPoints.Where(x => x.Id == id).ExecuteDeleteAsync(ct);
            if (count <= 0)
                return ResultDto<bool>.Failure("Entity doesnt exist in the Database!");

            return ResultDto<bool>.Success(true);
        }

        /// <summary>Points read from a script, linked to the areas they sit in.</summary>
        public static void Add(AppDbContext dbContext, Flow flow, IReadOnlyList<FlowPoint> points)
        {
            foreach (FlowPoint point in points)
                point.FlowId = flow.Id;

            dbContext.FlowPoints.AddRange(points);
        }
    }
}
