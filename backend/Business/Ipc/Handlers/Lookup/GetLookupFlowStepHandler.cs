using Core.Enums;
using Core.Helpers;
using Core.Models.Business;
using Core.Models.Dtos;
using Core.Models.Ipc;
using DataAccess;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Business.Ipc.Handlers
{
    public class GetLookupFlowStepHandler : IRequestHandler<GetLookupFlowStepQuery, ResultDto<LookupResponseDto>>
    {
        private static readonly Dictionary<StepResultKindEnum, FlowStepTypeEnum[]> ProducingTypesByKind = new()
        {
            [StepResultKindEnum.LOCATION] =
            [
                FlowStepTypeEnum.IMAGE_SEARCH,
                FlowStepTypeEnum.READ_TEXT,
            ],
            [StepResultKindEnum.VALUE] =
            [
                FlowStepTypeEnum.READ_TEXT,
                FlowStepTypeEnum.SYSTEM_COMMAND,
            ],
        };

        private IDbContextFactory<AppDbContext> _dbContextFactory;

        public GetLookupFlowStepHandler(IDbContextFactory<AppDbContext> dbContextFactory)
        {
            _dbContextFactory = dbContextFactory;
        }

        /// <summary>
        /// Returns the steps whose result can be read at <c>dto.FlowStepId</c> and that produce the
        /// requested kind of it, nearest first.
        ///
        /// Reachability is TreeStepHelper's rule rather than a plain ancestor walk, so this offers
        /// exactly what the validator accepts: a step under a Failure branch is not offered the
        /// search above it, because that search did not produce anything on the way down.
        ///
        /// In ADD mode there is no step row yet, so the caller passes the parent step id.
        /// </summary>
        public async Task<ResultDto<LookupResponseDto>> Handle(GetLookupFlowStepQuery request, CancellationToken ct)
        {
            LookupRequestDto dto = request.dto;

            if (dto.FlowStepId == null)
                return ResultDto<LookupResponseDto>.Success(new LookupResponseDto());

            FlowStepTypeEnum[] producingTypes = ProducingTypesByKind[dto.ResultKind ?? StepResultKindEnum.LOCATION];

            await using AppDbContext dbContext = await _dbContextFactory.CreateDbContextAsync(ct);

            // The whole tree of the root in one query, then walk the chain in memory.
            // RootId exists precisely so this never turns into a recursive CTE or N queries.
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
                .ReadableAncestors(byId, dto.FlowStepId.Value)
                .Where(x => producingTypes.Contains(x.Step.FlowStepType) && !dto.ExcludedIds.Contains(x.Step.Id))
                .Select(x => new LookupItemDto
                {
                    Value = x.Step.Id.ToString(),
                    Label = x.Step.Name,
                    Description = $"{x.Step.FlowStepType} · {x.Depth} level(s) up",
                })
                .ToList();

            if (!string.IsNullOrWhiteSpace(dto.SearchText))
                items = items.Where(x => x.Label.Contains(dto.SearchText, StringComparison.OrdinalIgnoreCase)).ToList();

            LookupResponseDto response = new LookupResponseDto { Data = items, TotalRecords = items.Count };
            return ResultDto<LookupResponseDto>.Success(response);
        }
    }
}
