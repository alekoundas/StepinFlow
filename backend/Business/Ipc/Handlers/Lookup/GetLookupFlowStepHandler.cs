using Core.Enums;
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
        /// Returns the ancestors of <c>dto.FlowStepId</c> that produce the requested kind of
        /// result, nearest first.
        ///
        /// A step reuses the result of the step it lives under, so only steps on its own parent
        /// chain are valid: anything else may not have run yet when this step executes.
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

            var steps = await dbContext.FlowSteps
                .AsNoTracking()
                .Where(x => x.RootId == rootId)
                .Select(x => new
                {
                    x.Id,
                    x.Name,
                    x.FlowStepType,
                    x.ParentFlowStepId,
                })
                .ToListAsync(ct);

            var stepsById = steps.ToDictionary(x => x.Id);

            List<LookupItemDto> items = new List<LookupItemDto>();
            int? currentId = dto.FlowStepId;
            int depth = 0;

            while (currentId != null && stepsById.TryGetValue(currentId.Value, out var step))
            {
                if (producingTypes.Contains(step.FlowStepType) && !dto.ExcludedIds.Contains(step.Id))
                {
                    items.Add(new LookupItemDto
                    {
                        Value = step.Id.ToString(),
                        Label = step.Name,
                        Description = $"{step.FlowStepType} · {depth} level(s) up",
                    });
                }

                currentId = step.ParentFlowStepId;
                depth++;
            }

            if (!string.IsNullOrWhiteSpace(dto.SearchText))
                items = items.Where(x => x.Label.Contains(dto.SearchText, StringComparison.OrdinalIgnoreCase)).ToList();

            LookupResponseDto response = new LookupResponseDto { Data = items, TotalRecords = items.Count };
            return ResultDto<LookupResponseDto>.Success(response);
        }
    }
}
