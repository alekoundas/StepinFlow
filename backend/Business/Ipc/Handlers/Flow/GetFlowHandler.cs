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
    public class GetFlowHandler : IRequestHandler<GetFlowQuery, GetFlowQueryResponse>
    {
        private readonly IMapper _mapper;
        private IDbContextFactory<AppDbContext> _dbContextFactory;

        public GetFlowHandler(IMapper mapper, IDataService dataService, IDbContextFactory<AppDbContext> dbContextFactory)
        {
            _mapper = mapper;
            _dbContextFactory = dbContextFactory;
        }

        public async Task<GetFlowQueryResponse> Handle(GetFlowQuery request, CancellationToken ct)
        {
            await using AppDbContext dbContext = await _dbContextFactory.CreateDbContextAsync();
            Flow? flow = await dbContext.Flows.FirstOrDefaultAsync(x=>x.Id == request.id);

            if (flow == null)
                return new GetFlowQueryResponse(null, false);

            FlowDto? flowDto = _mapper.Map<FlowDto>(flow);
            return new GetFlowQueryResponse(flowDto, true);
        }
    }
}
