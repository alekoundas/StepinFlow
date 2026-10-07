using Business.Validation;
using Core.Enums;
using Core.Models.Dtos;
using DataAccess;
using Microsoft.EntityFrameworkCore;

namespace Transport.Ipc.Handlers
{
    /// <summary>
    /// Error and warning counts for a page of flows.
    ///
    /// Separate from the list on purpose. Validating one flow loads every step it has, so folding
    /// this into Flow.getLazy would mean doing that for every row before anything renders. The
    /// list paints first and the badges arrive a beat later.
    ///
    /// Counts only: the list has no room for the messages, and fetching them would multiply the
    /// payload for something nobody reads until they open the flow.
    ///
    /// An empty id list means every flow. That keeps the caller from having to know which rows
    /// are currently on a page, and one cached answer serves paging and both views. It reads
    /// every step in the database to do it, which is nothing at this size but is the first thing
    /// to page if a install ever holds hundreds of flows.
    ///
    /// Validated the way one flow is, in one batch, so a badge and a refused Start cannot disagree.
    /// </summary>
    public class GetFlowHealthHandler
    {
        private readonly IDbContextFactory<AppDbContext> _dbContextFactory;
        private readonly IFlowValidationService _flowValidationService;

        public GetFlowHealthHandler(
            IDbContextFactory<AppDbContext> dbContextFactory,
            IFlowValidationService flowValidationService)
        {
            _dbContextFactory = dbContextFactory;
            _flowValidationService = flowValidationService;
        }

        public async Task<ResultDto<IReadOnlyList<FlowHealthDto>>> HandleAsync(FlowHealthRequestDto dto, CancellationToken ct)
        {
            await using AppDbContext dbContext = await _dbContextFactory.CreateDbContextAsync(ct);

            List<int> flowIds = dto.FlowIds;
            if (flowIds.Count == 0)
                flowIds = await dbContext.Flows.Select(x => x.Id).ToListAsync(ct);

            if (flowIds.Count == 0)
                return ResultDto<IReadOnlyList<FlowHealthDto>>.Success([]);

            IReadOnlyDictionary<int, FlowValidationResultDto> results = await _flowValidationService.ValidateAsync(dbContext, flowIds, ct);

            List<FlowHealthDto> health = flowIds
                .Select(flowId => new FlowHealthDto
                {
                    FlowId = flowId,
                    ErrorCount = results[flowId].Issues.Count(x => x.Severity == ValidationSeverityEnum.ERROR),
                    WarningCount = results[flowId].Issues.Count(x => x.Severity != ValidationSeverityEnum.ERROR),
                })
                .ToList();

            return ResultDto<IReadOnlyList<FlowHealthDto>>.Success(health);
        }
    }
}
