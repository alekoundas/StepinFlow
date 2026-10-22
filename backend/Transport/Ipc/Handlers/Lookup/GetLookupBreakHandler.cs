using Core.Enums;
using Core.Helpers;
using Core.Models.Business;
using Core.Models.Dtos;
using DataAccess;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

namespace Transport.Ipc.Handlers
{
    /// <summary>
    /// The steps a Break can leave from where it sits, nearest first. TreeStepHelper's rule, so this
    /// offers exactly what the validator accepts.
    ///
    /// Asked by the parent rather than by step, because in ADD mode there is no row yet: FlowId is
    /// the root flow, FlowStepId the parent step (null at the root, where there is nothing to leave).
    /// </summary>
    public class GetLookupBreakHandler
    {
        private readonly IDbContextFactory<AppDbContext> _dbContextFactory;

        public GetLookupBreakHandler(IDbContextFactory<AppDbContext> dbContextFactory)
        {
            _dbContextFactory = dbContextFactory;
        }

        public async Task<ResultDto<LookupResponseDto>> HandleAsync(LookupRequestDto dto, CancellationToken ct)
        {
            if (dto.FlowId == null || dto.FlowStepId == null)
                return ResultDto<LookupResponseDto>.Success(new LookupResponseDto());

            await using AppDbContext dbContext = await _dbContextFactory.CreateDbContextAsync(ct);

            Dictionary<int, StepChainNode> byId = await dbContext.FlowSteps
                .AsNoTracking()
                .Where(x => x.RootId == dto.FlowId)
                .Select(x => new StepChainNode(x.Id, x.ParentFlowStepId, x.FlowStepType, x.Name, x.OrderNumber))
                .ToDictionaryAsync(x => x.Id, ct);

            // Stands in for the Break under its parent, so ADD and EDIT ask the same question.
            StepChainNode position = new StepChainNode(0, dto.FlowStepId, FlowStepTypeEnum.BREAK, string.Empty, 0);
            byId[position.Id] = position;

            List<LookupItemDto> items = TreeStepHelper
                .BreakTargets(byId, position.Id)
                .Where(x => !dto.ExcludedIds.Contains(x.Id))
                .Select(x => new LookupItemDto
                {
                    Value = x.Id.ToString(CultureInfo.InvariantCulture),
                    Label = x.Name,
                    Description = x.FlowStepType.ToString(),
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
