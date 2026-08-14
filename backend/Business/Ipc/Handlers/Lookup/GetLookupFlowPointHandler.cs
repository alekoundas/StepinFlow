using Core.Models.Dtos;
using Core.Models.Ipc;
using DataAccess;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Business.Ipc.Handlers
{
    public class GetLookupFlowPointHandler : IRequestHandler<GetLookupFlowPointQuery, ResultDto<LookupResponseDto>>
    {
        private IDbContextFactory<AppDbContext> _dbContextFactory;

        public GetLookupFlowPointHandler(IDbContextFactory<AppDbContext> dbContextFactory)
        {
            _dbContextFactory = dbContextFactory;
        }

        public async Task<ResultDto<LookupResponseDto>> Handle(GetLookupFlowPointQuery request, CancellationToken ct)
        {
            LookupRequestDto dto = request.dto;

            if (dto.FlowId == null)
                return ResultDto<LookupResponseDto>.Success(new LookupResponseDto());

            await using AppDbContext dbContext = await _dbContextFactory.CreateDbContextAsync(ct);

            List<LookupItemDto> items = await dbContext.FlowPoints
                .AsNoTracking()
                .Where(x => x.FlowId == dto.FlowId)
                .Where(x => dto.SearchText == null || x.Name.Contains(dto.SearchText))
                .Where(x => !dto.ExcludedIds.Contains(x.Id))
                .OrderBy(x => x.Name)
                .Select(x => new LookupItemDto
                {
                    Value = x.Id.ToString(),
                    Label = x.Name,
                    Description = $"({x.LocationX}, {x.LocationY})",
                    // The Test button reads the coordinates straight off the selected option.
                    ExtraData = new
                    {
                        X = x.LocationX,
                        Y = x.LocationY,
                    },
                })
                .ToListAsync(ct);

            LookupResponseDto response = new LookupResponseDto { Data = items, TotalRecords = items.Count };
            return ResultDto<LookupResponseDto>.Success(response);
        }
    }
}
