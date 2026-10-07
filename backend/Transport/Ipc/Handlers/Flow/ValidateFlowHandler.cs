using Business.Flows.FlowValidationService;
using Core.Models.Dtos;
using DataAccess;
using Microsoft.EntityFrameworkCore;

namespace Transport.Ipc.Handlers
{
    /// <summary>
    /// One answer per flow rather than one per branch: checking whether a step can read another
    /// step's result needs the whole parent chain, so a per branch check would reload the same
    /// tree on every expand.
    /// </summary>
    public class ValidateFlowHandler
    {
        private readonly IDbContextFactory<AppDbContext> _dbContextFactory;
        private readonly IFlowValidationService _flowValidationService;

        public ValidateFlowHandler(IDbContextFactory<AppDbContext> dbContextFactory, IFlowValidationService flowValidationService)
        {
            _dbContextFactory = dbContextFactory;
            _flowValidationService = flowValidationService;
        }

        public async Task<ResultDto<FlowValidationResultDto>> HandleAsync(int id, CancellationToken ct)
        {
            await using AppDbContext dbContext = await _dbContextFactory.CreateDbContextAsync(ct);

            FlowValidationResultDto result = await _flowValidationService.ValidateAsync(dbContext, id, ct);

            return ResultDto<FlowValidationResultDto>.Success(result);
        }
    }
}
