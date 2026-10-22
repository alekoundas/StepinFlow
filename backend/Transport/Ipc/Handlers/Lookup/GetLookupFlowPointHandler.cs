using Core.Enums;
using Core.Models.Database;
using Core.Models.Dtos;
using DataAccess;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

namespace Transport.Ipc.Handlers
{
    public class GetLookupFlowPointHandler
    {
        private readonly IDbContextFactory<AppDbContext> _dbContextFactory;

        public GetLookupFlowPointHandler(IDbContextFactory<AppDbContext> dbContextFactory)
        {
            _dbContextFactory = dbContextFactory;
        }

        public async Task<ResultDto<LookupResponseDto>> HandleAsync(LookupRequestDto dto, CancellationToken ct)
        {
            if (dto.FlowId == null)
                return ResultDto<LookupResponseDto>.Success(new LookupResponseDto());

            await using AppDbContext dbContext = await _dbContextFactory.CreateDbContextAsync(ct);

            List<FlowPoint> points = await dbContext.FlowPoints
                .AsNoTracking()
                .Include(x => x.FlowArea)
                .Where(x => x.FlowId == dto.FlowId)
                .Where(x => dto.SearchText == null || x.Name.Contains(dto.SearchText))
                .Where(x => !dto.ExcludedIds.Contains(x.Id))
                .OrderBy(x => x.Name)
                .ToListAsync(ct);

            List<LookupItemDto> items = points
                .Select(x => new LookupItemDto
                {
                    Value = x.Id.ToString(CultureInfo.InvariantCulture),
                    Label = x.Name,
                    Description = Describe(x),
                })
                .ToList();

            LookupResponseDto response = new LookupResponseDto { Data = items, TotalRecords = items.Count };
            return ResultDto<LookupResponseDto>.Success(response);
        }

        // In the words the location form uses: what it is measured from, and in what.
        private static string Describe(FlowPoint point)
        {
            if (point.FlowArea == null)
                return FormattableString.Invariant($"{point.LocationX}, {point.LocationY} on screen");

            if (point.OffsetMode == AreaSizingModeEnum.RATIO)
                return FormattableString.Invariant($"{point.RatioX * 100:0.#}%, {point.RatioY * 100:0.#}% of {point.FlowArea.Name}");

            return FormattableString.Invariant($"{point.LocationX}, {point.LocationY} px in {point.FlowArea.Name}");
        }
    }
}
