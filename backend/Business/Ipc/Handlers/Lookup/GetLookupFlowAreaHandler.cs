using Core.Models.Dtos;
using Core.Models.Ipc;
using DataAccess;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Business.Ipc.Handlers
{
    public class GetLookupFlowAreaHandler : IRequestHandler<GetLookupFlowAreaQuery, ResultDto<LookupResponseDto>>
    {
        private IDbContextFactory<AppDbContext> _dbContextFactory;

        public GetLookupFlowAreaHandler(IDbContextFactory<AppDbContext> dbContextFactory)
        {
            _dbContextFactory = dbContextFactory;
        }

        public async Task<ResultDto<LookupResponseDto>> Handle(GetLookupFlowAreaQuery request, CancellationToken ct)
        {
            LookupRequestDto dto = request.dto;

            if (dto.FlowId == null)
                return ResultDto<LookupResponseDto>.Success(new LookupResponseDto());

            await using AppDbContext dbContext = await _dbContextFactory.CreateDbContextAsync(ct);

            List<LookupItemDto> items = await dbContext.FlowAreas
                .AsNoTracking()
                .Where(x => x.FlowId == dto.FlowId)
                .Where(x => dto.FlowAreaType == null || x.Type == dto.FlowAreaType)
                .Where(x => dto.SearchText == null || x.Name.Contains(dto.SearchText))
                .Where(x => !dto.ExcludedIds.Contains(x.Id))
                .OrderBy(x => x.Name)
                .Select(x => new LookupItemDto
                {
                    Value = x.Id.ToString(),
                    Label = x.Name,
                    Description = x.Type.ToString(),
                })
                .ToListAsync(ct);

            return ResultDto<LookupResponseDto>.Success(new LookupResponseDto { Data = items, TotalRecords = items.Count });
        }
    }
}
