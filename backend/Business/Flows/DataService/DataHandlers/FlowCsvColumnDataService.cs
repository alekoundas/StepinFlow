using Business.Flows.FlowValidationService;
using Business.Repository;
using Core.Helpers;
using Core.Models.Database;
using Core.Models.Dtos;
using DataAccess;
using Microsoft.EntityFrameworkCore;

namespace Business.Flows.DataService.DataHandlers
{
    /// <summary>
    /// Every change to a flow's inputs. A secret keeps no value: what was typed is never stored.
    /// </summary>
    public sealed class FlowCsvColumnDataService : BaseDataService
    {
        private const string NAME_MISSING = "An input needs a name: the steps type it as {{name}}.";

        private readonly IGitRepositoryService _gitRepositoryService;

        public FlowCsvColumnDataService(IDbContextFactory<AppDbContext> dbContextFactory, IFlowValidationService flowValidationService, IGitRepositoryService gitRepositoryService)
            : base(dbContextFactory, flowValidationService)
        {
            _gitRepositoryService = gitRepositoryService;
        }


        // ================================================================
        // Public methods
        // ================================================================

        public async Task<ResultDto<int>> CreateAsync(FlowCsvColumn input, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(input.Name))
                return ResultDto<int>.Failure(NAME_MISSING);

            await using AppDbContext dbContext = await DbContextFactory.CreateDbContextAsync(ct);

            input.Id = 0;
            input.Name = FlowNameHelper.MakeUnique(input.Name, await TakenNamesAsync(dbContext, input.FlowId, ct));
            input.OrderNumber = await NextOrderNumberAsync(dbContext, input.FlowId, ct);

            if (input.IsSecret)
                input.DefaultValue = string.Empty;

            dbContext.FlowCsvColumns.Add(input);
            await dbContext.SaveChangesAsync(ct);
            _gitRepositoryService.Changed(input.FlowId);

            return ResultDto<int>.Success(input.Id);
        }

        // Renaming does not rename it in the steps that type it. The validator says which ones
        // still use the old name.
        public async Task<ResultDto<FlowCsvColumn>> UpdateAsync(FlowCsvColumnDto dto, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(dto.Name))
                return ResultDto<FlowCsvColumn>.Failure(NAME_MISSING);

            await using AppDbContext dbContext = await DbContextFactory.CreateDbContextAsync(ct);

            FlowCsvColumn? input = await dbContext.FlowCsvColumns.FirstOrDefaultAsync(x => x.Id == dto.Id, ct);
            if (input == null)
                return ResultDto<FlowCsvColumn>.Failure("Entity doesnt exist in the Database!");

            HashSet<string> taken = await TakenNamesAsync(dbContext, input.FlowId, ct);
            taken.Remove(input.Name);

            input.Name = FlowNameHelper.MakeUnique(dto.Name, taken);
            input.IsSecret = dto.IsSecret;
            input.DefaultValue = dto.DefaultValue;

            if (input.IsSecret)
                input.DefaultValue = string.Empty;

            await dbContext.SaveChangesAsync(ct);
            _gitRepositoryService.Changed(input.FlowId);

            return ResultDto<FlowCsvColumn>.Success(input);
        }

        public async Task<ResultDto<bool>> DeleteAsync(int id, CancellationToken ct)
        {
            await using AppDbContext dbContext = await DbContextFactory.CreateDbContextAsync(ct);

            int? flowId = await dbContext.FlowCsvColumns.Where(x => x.Id == id).Select(x => (int?)x.FlowId).FirstOrDefaultAsync(ct);

            int count = await dbContext.FlowCsvColumns.Where(x => x.Id == id).ExecuteDeleteAsync(ct);
            if (count <= 0 || flowId == null)
                return ResultDto<bool>.Failure("Entity doesnt exist in the Database!");

            _gitRepositoryService.Changed(flowId.Value);

            return ResultDto<bool>.Success(true);
        }


        // ================================================================
        // Private methods
        // ================================================================

        private static async Task<int> NextOrderNumberAsync(AppDbContext dbContext, int flowId, CancellationToken ct)
        {
            int? last = await dbContext.FlowCsvColumns
                .Where(x => x.FlowId == flowId)
                .MaxAsync(x => (int?)x.OrderNumber, ct);

            if (last == null)
                return 0;

            return last.Value + 1;
        }
    }
}
