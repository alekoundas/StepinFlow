using AutoMapper;
using Core.Models.Database;
using Core.Models.Dtos;
using Core.Models.Ipc;
using DataAccess;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Business.Ipc.Handlers
{
    public class GetLazySubFlowHandler : IRequestHandler<GetLazySubFlowQuery, ResultDto<LazyResponseDto<SubFlowDto>>>
    {
        private readonly IMapper _mapper;
        private IDbContextFactory<AppDbContext> _dbContextFactory;

        public GetLazySubFlowHandler(IMapper mapper, IDbContextFactory<AppDbContext> dbContextFactory)
        {
            _mapper = mapper;
            _dbContextFactory = dbContextFactory;
        }

        public async Task<ResultDto<LazyResponseDto<SubFlowDto>>> Handle(GetLazySubFlowQuery request, CancellationToken ct)
        {
            await using AppDbContext dbContext = await _dbContextFactory.CreateDbContextAsync(ct);
            List<SubFlow> subFlows = await dbContext.SubFlows.AsNoTracking().ToListAsync(ct);

            List<SubFlowDto> subFlowDtos = _mapper.Map<List<SubFlowDto>>(subFlows);
            LazyResponseDto<SubFlowDto> dataTableResponseDto = new LazyResponseDto<SubFlowDto>();
            dataTableResponseDto.Data = subFlowDtos;
            dataTableResponseDto.TotalRecords = subFlowDtos.Count;

            return ResultDto<LazyResponseDto<SubFlowDto>>.Success(dataTableResponseDto);
        }
    }
}
