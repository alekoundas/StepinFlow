using AutoMapper;
using Business.DataService.Services;
using Core.Models.Database;
using Core.Models.Dtos;
using Core.Models.Ipc.Commands.Flow;
using Core.Models.Ipc.Commands.FlowStep;
using DataAccess;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Business.Ipc.Handlers
{
    public class GetFlowStepHandler : IRequestHandler<GetFlowStepQuery, GetFlowStepQueryResponse>
    {
        private readonly IMapper _mapper;
        private IDbContextFactory<AppDbContext> _dbContextFactory;

        public GetFlowStepHandler(IMapper mapper, IDataService dataService, IDbContextFactory<AppDbContext> dbContextFactory)
        {
            _mapper = mapper;
            _dbContextFactory = dbContextFactory;
        }

        public async Task<GetFlowStepQueryResponse> Handle(GetFlowStepQuery request, CancellationToken ct)
        {
            await using AppDbContext dbContext = await _dbContextFactory.CreateDbContextAsync();
            FlowStep? flowStep = await dbContext.FlowSteps.FirstOrDefaultAsync(x=>x.Id == request.id);

            if (flowStep == null)
                return new GetFlowStepQueryResponse(null);

            FlowStepDto? flowStepDto = _mapper.Map<FlowStepDto>(flowStep);
            return new GetFlowStepQueryResponse(flowStepDto);
        }
    }
}
