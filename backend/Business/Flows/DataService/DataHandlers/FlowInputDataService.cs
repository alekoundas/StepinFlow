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
    /// Every change to a flow's inputs. A secret's value is kept here, in the database only, and
    /// never sent back.
    /// </summary>
    public sealed class FlowInputDataService : BaseDataService
    {
        private const string NAME_MISSING = "An input needs a name: the steps type it as {{name}}.";

        private readonly IGitRepositoryService _gitRepositoryService;

        public FlowInputDataService(IDbContextFactory<AppDbContext> dbContextFactory, IFlowValidationService flowValidationService, IGitRepositoryService gitRepositoryService)
            : base(dbContextFactory, flowValidationService)
        {
            _gitRepositoryService = gitRepositoryService;
        }


        // ================================================================
        // Public methods
        // ================================================================

        public async Task<ResultDto<int>> CreateAsync(FlowInput input, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(input.Name))
                return ResultDto<int>.Failure(NAME_MISSING);

            await using AppDbContext dbContext = await DbContextFactory.CreateDbContextAsync(ct);

            input.Id = 0;
            input.Name = FlowNameHelper.MakeUnique(input.Name, await TakenNamesAsync(dbContext, input.FlowId, ct));
            input.OrderNumber = await NextOrderNumberAsync(dbContext, input.FlowId, ct);

            if (input.IsSecret)
                input.DefaultValue = string.Empty;
            else
                input.SecretValue = string.Empty;

            dbContext.FlowInputs.Add(input);
            await dbContext.SaveChangesAsync(ct);
            _gitRepositoryService.Changed(input.FlowId);

            return ResultDto<int>.Success(input.Id);
        }

        // Renaming does not rename it in the steps that type it. The validator says which ones
        // still use the old name.
        public async Task<ResultDto<FlowInput>> UpdateAsync(FlowInputDto dto, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(dto.Name))
                return ResultDto<FlowInput>.Failure(NAME_MISSING);

            await using AppDbContext dbContext = await DbContextFactory.CreateDbContextAsync(ct);

            FlowInput? input = await dbContext.FlowInputs.FirstOrDefaultAsync(x => x.Id == dto.Id, ct);
            if (input == null)
                return ResultDto<FlowInput>.Failure("Entity doesnt exist in the Database!");

            HashSet<string> taken = await TakenNamesAsync(dbContext, input.FlowId, ct);
            taken.Remove(input.Name);

            input.Name = FlowNameHelper.MakeUnique(dto.Name, taken);
            input.IsSecret = dto.IsSecret;
            input.DefaultValue = dto.DefaultValue;

            // A secret's form never holds the saved value, so leaving it empty keeps it.
            if (input.IsSecret)
            {
                input.DefaultValue = string.Empty;
                if (dto.NewSecretValue.Length > 0)
                    input.SecretValue = dto.NewSecretValue;
            }
            else
            {
                input.SecretValue = string.Empty;
            }

            await dbContext.SaveChangesAsync(ct);
            _gitRepositoryService.Changed(input.FlowId);

            return ResultDto<FlowInput>.Success(input);
        }

        public async Task<ResultDto<bool>> DeleteAsync(int id, CancellationToken ct)
        {
            await using AppDbContext dbContext = await DbContextFactory.CreateDbContextAsync(ct);

            int? flowId = await dbContext.FlowInputs.Where(x => x.Id == id).Select(x => (int?)x.FlowId).FirstOrDefaultAsync(ct);

            int count = await dbContext.FlowInputs.Where(x => x.Id == id).ExecuteDeleteAsync(ct);
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
            int? last = await dbContext.FlowInputs
                .Where(x => x.FlowId == flowId)
                .MaxAsync(x => (int?)x.OrderNumber, ct);

            if (last == null)
                return 0;

            return last.Value + 1;
        }
    }
}
