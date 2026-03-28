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
    public class GetFlowStepHandler : IRequestHandler<GetFlowStepQuery, ResultDto<FlowStepDto>>
    {
        private readonly IMapper _mapper;
        private IDbContextFactory<AppDbContext> _dbContextFactory;

        public GetFlowStepHandler(IMapper mapper, IDataService dataService, IDbContextFactory<AppDbContext> dbContextFactory)
        {
            _mapper = mapper;
            _dbContextFactory = dbContextFactory;
        }

        public async Task<ResultDto<FlowStepDto>> Handle(GetFlowStepQuery request, CancellationToken ct)
        {
            await using AppDbContext dbContext = await _dbContextFactory.CreateDbContextAsync();
            FlowStep? flowStep = await dbContext.FlowSteps.FirstOrDefaultAsync(x => x.Id == request.id);

            if (flowStep == null)
                return ResultDto<FlowStepDto>.Failure("Entity doesnt exist in the Database!");

            FlowStepDto? flowStepDto = _mapper.Map<FlowStepDto>(flowStep);
            return ResultDto<FlowStepDto>.Success(flowStepDto);
        }
    }
}
