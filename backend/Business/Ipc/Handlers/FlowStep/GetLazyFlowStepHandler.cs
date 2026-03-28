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
    public class GetLazyFlowStepHandler : IRequestHandler<GetLazyFlowQuery, ResultDto<LazyResponseDto<FlowDto>>>
    {
        private readonly IMapper _mapper;
        private IDbContextFactory<AppDbContext> _dbContextFactory;

        public GetLazyFlowStepHandler(IMapper mapper, IDataService dataService, IDbContextFactory<AppDbContext> dbContextFactory)
        {
            _mapper = mapper;
            _dbContextFactory = dbContextFactory;
        }

        public async Task<ResultDto<LazyResponseDto<FlowDto>>> Handle(GetLazyFlowQuery request, CancellationToken ct)
        {
            await using AppDbContext dbContext = await _dbContextFactory.CreateDbContextAsync();
            List<Flow> flows = await dbContext.Flows.ToListAsync();

            List<FlowDto> flowDtos = _mapper.Map<List<FlowDto>>(flows);
            LazyResponseDto<FlowDto> dataTableResponseDto = new LazyResponseDto<FlowDto>();
            dataTableResponseDto.Data = flowDtos;
            dataTableResponseDto.TotalRecords = flowDtos.Count;

            return ResultDto<LazyResponseDto<FlowDto>>.Success(dataTableResponseDto);
        }
    }
}
