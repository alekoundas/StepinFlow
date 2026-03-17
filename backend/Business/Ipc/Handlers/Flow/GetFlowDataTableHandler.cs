using AutoMapper;
using Business.DataService.Services;
using Core.Models.Database;
using Core.Models.Dtos;
using Core.Models.Ipc.Commands.Flow;
using DataAccess;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Business.Ipc.Handlers
{
    public class GetFlowDataTableHandler : IRequestHandler<GetFlowDataTableQuery, GetFlowDataTableQueryResponse>
    {
        private readonly IMapper _mapper;
        private IDbContextFactory<AppDbContext> _dbContextFactory;

        public GetFlowDataTableHandler(IMapper mapper, IDataService dataService, IDbContextFactory<AppDbContext> dbContextFactory)
        {
            _mapper = mapper;
            _dbContextFactory = dbContextFactory;
        }

        public async Task<GetFlowDataTableQueryResponse> Handle(GetFlowDataTableQuery request, CancellationToken ct)
        {
            await using AppDbContext dbContext = await _dbContextFactory.CreateDbContextAsync();
            List<Flow>? flows = await dbContext.Flows.ToListAsync();


            List<FlowDto>? flowDtos = _mapper.Map<List<FlowDto>>(flows);
            return new GetFlowDataTableQueryResponse(flowDtos, flowDtos.Count);
        }
    }
}
