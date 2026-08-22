using Core.Helpers;
using Core.Models.Business;
using Core.Models.Dtos;
using Core.Models.Ipc;
using DataAccess;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Business.Ipc.Handlers
{
    /// <summary>
    /// The steps whose failure a Notify step can report from where it sits, nearest first.
    ///
    /// The mirror of Lookup.flowStep: that one walks down through Success branches, because a
    /// result only exists if the step succeeded. This one walks down through Failure branches,
    /// because a Notify step only runs at all when something above it failed.
    ///
    /// Empty is a correct answer, and a common one - a Notify step that is not on any failure path
    /// has nothing to report, and the form treats an empty list as "there is no failure here".
    ///
    /// In ADD mode there is no step row yet, so the caller passes the parent step id.
    /// </summary>
    public class GetLookupFailedStepHandler : IRequestHandler<GetLookupFailedStepQuery, ResultDto<LookupResponseDto>>
    {
        private readonly IDbContextFactory<AppDbContext> _dbContextFactory;

        public GetLookupFailedStepHandler(IDbContextFactory<AppDbContext> dbContextFactory)
        {
            _dbContextFactory = dbContextFactory;
        }

        public async Task<ResultDto<LookupResponseDto>> Handle(GetLookupFailedStepQuery request, CancellationToken ct)
        {
            LookupRequestDto dto = request.dto;

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
                .Select(x => new StepChainNode(x.Id, x.ParentFlowStepId, x.FlowStepType, x.Name))
                .ToDictionaryAsync(x => x.Id, ct);

            List<LookupItemDto> items = TreeStepHelper
                .FailedAncestors(byId, dto.FlowStepId.Value)
                .Where(x => !dto.ExcludedIds.Contains(x.Step.Id))
                .Select(x => new LookupItemDto
                {
                    Value = x.Step.Id.ToString(),
                    Label = x.Step.Name,
                    Description = $"{x.Step.FlowStepType} · {x.Depth} level(s) up",
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
