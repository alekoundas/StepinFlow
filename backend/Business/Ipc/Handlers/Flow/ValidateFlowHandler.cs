using Business.Services.FlowValidationService;
using Core.Models.Database;
using Core.Models.Dtos;
using Core.Models.Ipc;
using DataAccess;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Business.Ipc.Handlers
{
    /// <summary>
    /// One answer per flow rather than one per branch: checking whether a step can read another
    /// step's result needs the whole parent chain, so a per branch check would reload the same
    /// tree on every expand.
    /// </summary>
    public class ValidateFlowHandler : IRequestHandler<ValidateFlowQuery, ResultDto<FlowValidationResultDto>>
    {
        private readonly IDbContextFactory<AppDbContext> _dbContextFactory;
        private readonly IFlowValidator _flowValidator;

        public ValidateFlowHandler(IDbContextFactory<AppDbContext> dbContextFactory, IFlowValidator flowValidator)
        {
            _dbContextFactory = dbContextFactory;
            _flowValidator = flowValidator;
        }

        public async Task<ResultDto<FlowValidationResultDto>> Handle(ValidateFlowQuery request, CancellationToken ct)
        {
            await using AppDbContext dbContext = await _dbContextFactory.CreateDbContextAsync(ct);

            List<FlowStep> steps = await dbContext.FlowSteps
                .AsNoTracking()
                .Where(x => x.RootId == request.id)
                .ToListAsync(ct);

            // Counted rather than Included: the templates themselves are megabytes and only their
            // number matters here.
            var templateCounts = await dbContext.FlowStepImages
                .AsNoTracking()
                .Where(x => x.FlowStep.RootId == request.id)
                .GroupBy(x => x.FlowStepId)
                .Select(x => new { FlowStepId = x.Key, Count = x.Count() })
                .ToListAsync(ct);

            FlowValidationResultDto result = _flowValidator.Validate(
                steps,
                templateCounts.ToDictionary(x => x.FlowStepId, x => x.Count));

            return ResultDto<FlowValidationResultDto>.Success(result);
        }
    }
}
