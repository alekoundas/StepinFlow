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
    public class GetFlowSearchAreaHandler : IRequestHandler<GetFlowSearchAreaQuery, ResultDto<FlowSearchAreaDto>>
    {
        private readonly IMapper _mapper;
        private IDbContextFactory<AppDbContext> _dbContextFactory;

        public GetFlowSearchAreaHandler(IMapper mapper, IDataService dataService, IDbContextFactory<AppDbContext> dbContextFactory)
        {
            _mapper = mapper;
            _dbContextFactory = dbContextFactory;
        }

        public async Task<ResultDto<FlowSearchAreaDto>> Handle(GetFlowSearchAreaQuery request, CancellationToken ct)
        {
            await using AppDbContext dbContext = await _dbContextFactory.CreateDbContextAsync();
            FlowSearchArea? flowSearchArea = await dbContext.FlowSearchAreas.FirstOrDefaultAsync(x=>x.Id == request.id);

            if (flowSearchArea == null)
            return ResultDto<FlowSearchAreaDto>.Failure("Entity doesnt exist in the Database!");

            FlowSearchAreaDto? flowSearchAreaDto = _mapper.Map<FlowSearchAreaDto>(flowSearchArea);
            return ResultDto<FlowSearchAreaDto>.Success(flowSearchAreaDto);
        }
    }
}
