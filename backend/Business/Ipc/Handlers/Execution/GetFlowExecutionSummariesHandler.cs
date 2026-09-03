using Core.Enums;
using Core.Models.Dtos;
using Core.Models.Dtos.Database;
using Core.Models.Ipc;

using DataAccess;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Business.Ipc.Handlers.Execution
{
    /// <summary>
    /// Every flow with the shape of its run history, for the executions list.
    ///
    /// Flows that have never run are included: "never run" is an answer, and leaving them out would
    /// make the list disagree with the flows page about how many flows exist.
    /// </summary>
    public class GetFlowExecutionSummariesHandler : IRequestHandler<GetFlowExecutionSummariesQuery, ResultDto<List<FlowExecutionSummaryDto>>>
    {
        // Read once and grouped in memory rather than a query per flow. Nothing prunes the table
        // yet, so this is also what stops a year of history being loaded to draw ten bars.
        private const int _maxRuns = 500;
        private const int _recentOutcomes = 10;

        private readonly IDbContextFactory<AppDbContext> _dbContextFactory;

        public GetFlowExecutionSummariesHandler(IDbContextFactory<AppDbContext> dbContextFactory)
        {
            _dbContextFactory = dbContextFactory;
        }

        public async Task<ResultDto<List<FlowExecutionSummaryDto>>> Handle(GetFlowExecutionSummariesQuery request, CancellationToken ct)
        {
            await using AppDbContext dbContext = await _dbContextFactory.CreateDbContextAsync(ct);

            var flows = await dbContext.Flows
                .AsNoTracking()
                .OrderBy(x => x.Name)
                .Select(x => new { x.Id, x.Name, x.IsSubFlow })
                .ToListAsync(ct);

            var runs = await dbContext.Executions
                .AsNoTracking()
                .OrderByDescending(x => x.Id)
                .Take(_maxRuns)
                .Select(x => new { x.FlowId, x.Status, x.CreatedOn })
                .ToListAsync(ct);

            List<FlowExecutionSummaryDto> summaries = new List<FlowExecutionSummaryDto>();

            foreach (var flow in flows)
            {
                // Newest first, which is how they arrived.
                var flowRuns = runs.Where(x => x.FlowId == flow.Id).ToList();

                FlowExecutionSummaryDto summary = new FlowExecutionSummaryDto
                {
                    FlowId = flow.Id,
                    FlowName = flow.Name,
                    IsSubFlow = flow.IsSubFlow,
                    RunCount = flowRuns.Count,
                    CompletedCount = flowRuns.Count(x => x.Status == ExecutionStatusEnum.COMPLETED),
                    LastRunOn = flowRuns.Count > 0 ? flowRuns[0].CreatedOn : null,
                    LastStatus = flowRuns.Count > 0 ? flowRuns[0].Status : null,
                    RecentOutcomes = flowRuns
                        .Take(_recentOutcomes)
                        .Select(x => x.Status)
                        .Reverse()
                        .ToList(),
                };

                summaries.Add(summary);
            }

            return ResultDto<List<FlowExecutionSummaryDto>>.Success(summaries);
        }
    }
}
