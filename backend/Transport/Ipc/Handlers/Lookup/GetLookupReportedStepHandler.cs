using Core.Helpers;
using Core.Models.Business;
using Core.Models.Dtos;
using DataAccess;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

namespace Transport.Ipc.Handlers
{
    /// <summary>
    /// The steps whose result a Notify step can include from where it sits, nearest first: the
    /// ones it sits under through a Success or a Failure, since a result is kept for everything
    /// under its step whichever way it went.
    ///
    /// Empty is a correct answer - a Notify step under no check has no result to include, and the
    /// form treats an empty list as "there is nothing here to report on".
    ///
    /// In ADD mode there is no step row yet, so the caller passes the parent step id.
    /// </summary>
    public class GetLookupReportedStepHandler
    {
        private readonly IDbContextFactory<AppDbContext> _dbContextFactory;

        public GetLookupReportedStepHandler(IDbContextFactory<AppDbContext> dbContextFactory)
        {
            _dbContextFactory = dbContextFactory;
        }

        public async Task<ResultDto<LookupResponseDto>> HandleAsync(LookupRequestDto dto, CancellationToken ct)
        {
            if (dto.FlowStepId == null)
                return ResultDto<LookupResponseDto>.Success(new LookupResponseDto());

            await using AppDbContext dbContext = await _dbContextFactory.CreateDbContextAsync(ct);

            // The whole tree of the root in one query, then walk the chain in memory. RootId is
            // what keeps this from becoming a recursive CTE or N queries.
            int rootId = await dbContext.FlowSteps
                .AsNoTracking()
                .Where(x => x.Id == dto.FlowStepId)
                .Select(x => x.RootId)
                .FirstOrDefaultAsync(ct);

            Dictionary<int, StepChainNode> byId = await dbContext.FlowSteps
                .AsNoTracking()
                .Where(x => x.RootId == rootId)
                .Select(x => new StepChainNode(x.Id, x.ParentFlowStepId, x.FlowStepType, x.Name, x.OrderNumber))
                .ToDictionaryAsync(x => x.Id, ct);

            List<LookupItemDto> items = TreeStepHelper
                .BranchedAncestors(byId, dto.FlowStepId.Value)
                .Where(x => !dto.ExcludedIds.Contains(x.Step.Id))
                .Select(x => new LookupItemDto
                {
                    Value = x.Step.Id.ToString(CultureInfo.InvariantCulture),
                    Label = x.Step.Name,
                    Description = $"{x.Step.FlowStepType} · under its {TreeStepHelper.BranchName(x.Branch)} · {x.Depth} level(s) up",
                })
                .ToList();

            if (!string.IsNullOrWhiteSpace(dto.SearchText))
                items = items.Where(x => x.Label.Contains(dto.SearchText, StringComparison.OrdinalIgnoreCase)).ToList();

            return ResultDto<LookupResponseDto>.Success(new LookupResponseDto
            {
                Data = items,
                TotalRecords = items.Count,
            });
        }
    }
}
