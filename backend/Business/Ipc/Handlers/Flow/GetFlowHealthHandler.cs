using Business.Services.FlowValidationService;
using Core.Enums;
using Core.Models.Database;
using Core.Models.Dtos;
using Core.Models.Ipc;
using DataAccess;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Business.Ipc.Handlers
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
    /// </summary>
    public class GetFlowHealthHandler : IRequestHandler<GetFlowHealthQuery, ResultDto<IReadOnlyList<FlowHealthDto>>>
    {
        private readonly IDbContextFactory<AppDbContext> _dbContextFactory;
        private readonly IFlowValidator _flowValidator;

        public GetFlowHealthHandler(
            IDbContextFactory<AppDbContext> dbContextFactory,
            IFlowValidator flowValidator)
        {
            _dbContextFactory = dbContextFactory;
            _flowValidator = flowValidator;
        }

        public async Task<ResultDto<IReadOnlyList<FlowHealthDto>>> Handle(GetFlowHealthQuery request, CancellationToken ct)
        {
            List<int> requested = request.dto.FlowIds;
            bool all = requested.Count == 0;

            await using AppDbContext dbContext = await _dbContextFactory.CreateDbContextAsync(ct);

            List<int> flowIds = all
                ? await dbContext.Flows.Select(x => x.Id).ToListAsync(ct)
                : requested;

            if (flowIds.Count == 0)
                return ResultDto<IReadOnlyList<FlowHealthDto>>.Success([]);

            // Every step of every flow asked about in one query, then split in memory. One round
            // trip whatever the page size.
            List<FlowStep> steps = await dbContext.FlowSteps
                .AsNoTracking()
                .Where(x => all || flowIds.Contains(x.RootId))
                .ToListAsync(ct);

            Dictionary<int, int> templateCounts = await dbContext.FlowStepImages
                .AsNoTracking()
                .Where(x => all || flowIds.Contains(x.FlowStep.RootId))
                .GroupBy(x => x.FlowStepId)
                .Select(x => new { FlowStepId = x.Key, Count = x.Count() })
                .ToDictionaryAsync(x => x.FlowStepId, x => x.Count, ct);

            ILookup<int, FlowStep> stepsByRoot = steps.ToLookup(x => x.RootId);

            List<FlowHealthDto> health = flowIds
                .Select(flowId =>
                {
                    FlowValidationResultDto result =
                        _flowValidator.Validate(stepsByRoot[flowId].ToList(), templateCounts);

                    return new FlowHealthDto
                    {
                        FlowId = flowId,
                        ErrorCount = result.Issues.Count(x => x.Severity == ValidationSeverityEnum.ERROR),
                        WarningCount = result.Issues.Count(x => x.Severity != ValidationSeverityEnum.ERROR),
                    };
                })
                .ToList();

            return ResultDto<IReadOnlyList<FlowHealthDto>>.Success(health);
        }
    }
}
