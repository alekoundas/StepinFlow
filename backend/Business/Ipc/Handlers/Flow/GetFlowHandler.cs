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
    public class GetFlowHandler : IRequestHandler<GetFlowQuery, ResultDto<FlowDto>>
    {
        private readonly IMapper _mapper;
        private IDbContextFactory<AppDbContext> _dbContextFactory;

        public GetFlowHandler(IMapper mapper, IDataService dataService, IDbContextFactory<AppDbContext> dbContextFactory)
        {
            _mapper = mapper;
            _dbContextFactory = dbContextFactory;
        }

        public async Task<ResultDto<FlowDto>> Handle(GetFlowQuery request, CancellationToken ct)
        {
            await using AppDbContext dbContext = await _dbContextFactory.CreateDbContextAsync();
            Flow? flow = await dbContext.Flows.FirstOrDefaultAsync(x=>x.Id == request.id);

            if (flow == null)
            return ResultDto<FlowDto>.Failure("Entity doesnt exist in the Database!");

            FlowDto? flowDto = _mapper.Map<FlowDto>(flow);
            return ResultDto<FlowDto>.Success(flowDto);
        }
    }
}
