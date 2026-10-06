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
    /// The steps a Go Back can return to from where it sits, nearest first. TreeStepHelper's rule,
    /// so this offers exactly what the validator accepts.
    ///
    /// Asked by position rather than by step, because in ADD mode there is no row yet: FlowId is the
    /// root flow, FlowStepId the parent step (null at the root), OrderNumber where it sits among
    /// its siblings.
    /// </summary>
    public class GetLookupGoBackHandler
    {
        private readonly IDbContextFactory<AppDbContext> _dbContextFactory;

        public GetLookupGoBackHandler(IDbContextFactory<AppDbContext> dbContextFactory)
        {
            _dbContextFactory = dbContextFactory;
        }

        public async Task<ResultDto<LookupResponseDto>> HandleAsync(LookupRequestDto dto, CancellationToken ct)
        {
            if (dto.FlowId == null || dto.OrderNumber == null)
                return ResultDto<LookupResponseDto>.Success(new LookupResponseDto());

            await using AppDbContext dbContext = await _dbContextFactory.CreateDbContextAsync(ct);

            Dictionary<int, StepChainNode> byId = await dbContext.FlowSteps
                .AsNoTracking()
                .Where(x => x.RootId == dto.FlowId)
                .Select(x => new StepChainNode(x.Id, x.ParentFlowStepId, x.FlowStepType, x.Name, x.OrderNumber))
                .ToDictionaryAsync(x => x.Id, ct);

            // Stands in for the Go Back at its position, so ADD and EDIT ask the same question. In
            // EDIT the saved row shares its order number, and only earlier siblings count.
            StepChainNode position = new StepChainNode(0, dto.FlowStepId, FlowStepTypeEnum.GO_BACK, string.Empty, dto.OrderNumber.Value);
            byId[position.Id] = position;

            List<LookupItemDto> items = TreeStepHelper
                .GoBackTargets(byId, position.Id)
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
