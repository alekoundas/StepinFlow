using AutoMapper;
using Business.DataService.Services;
using Core.Models.Database;
using Core.Models.Dtos;
using Core.Models.Ipc;
using DataAccess;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Business.Ipc.Handlers
{
    public class GetLazyFlowSearchAreaHandler : IRequestHandler<GetLazyFlowSearchAreaQuery, ResultDto<LazyResponseDto<FlowSearchAreaDto>>>
    {
        private readonly IMapper _mapper;
        private IDbContextFactory<AppDbContext> _dbContextFactory;

        public GetLazyFlowSearchAreaHandler(IMapper mapper, IDataService dataService, IDbContextFactory<AppDbContext> dbContextFactory)
        {
            _mapper = mapper;
            _dbContextFactory = dbContextFactory;
        }

        public async Task<ResultDto<LazyResponseDto<FlowSearchAreaDto>>> Handle(GetLazyFlowSearchAreaQuery request, CancellationToken ct)
        {
            await using AppDbContext dbContext = await _dbContextFactory.CreateDbContextAsync();
            List<FlowSearchArea> flowSearchAreas = await dbContext.FlowSearchAreas.ToListAsync();

            List<FlowSearchAreaDto> flowSearchAreaDtos = _mapper.Map<List<FlowSearchAreaDto>>(flowSearchAreas);
            LazyResponseDto<FlowSearchAreaDto> dataTableResponseDto = new LazyResponseDto<FlowSearchAreaDto>();
            dataTableResponseDto.Data = flowSearchAreaDtos;
            dataTableResponseDto.TotalRecords = flowSearchAreaDtos.Count;

            return ResultDto<LazyResponseDto<FlowSearchAreaDto>>.Success(dataTableResponseDto);
        }
    }
}
