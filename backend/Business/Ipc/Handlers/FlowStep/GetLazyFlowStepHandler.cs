using AutoMapper;
using Core.Models.Database;
using Core.Models.Dtos;
using Core.Models.Ipc;
using DataAccess;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Business.Ipc.Handlers
{
    public class GetLazyFlowStepHandler : IRequestHandler<GetLazyStepFlowQuery, ResultDto<LazyResponseDto<FlowStepDto>>>
    {
        private readonly IMapper _mapper;
        private IDbContextFactory<AppDbContext> _dbContextFactory;

        public GetLazyFlowStepHandler(IMapper mapper, IDbContextFactory<AppDbContext> dbContextFactory)
        {
            _mapper = mapper;
            _dbContextFactory = dbContextFactory;
        }

        public async Task<ResultDto<LazyResponseDto<FlowStepDto>>> Handle(GetLazyStepFlowQuery request, CancellationToken ct)
        {
            await using AppDbContext dbContext = await _dbContextFactory.CreateDbContextAsync(ct);
            List<FlowStep> flowSteps = await dbContext.FlowSteps.AsNoTracking().ToListAsync(ct);

            List<FlowStepDto> flowStepDtos = _mapper.Map<List<FlowStepDto>>(flowSteps);
            LazyResponseDto<FlowStepDto> dataTableResponseDto = new LazyResponseDto<FlowStepDto>();
            dataTableResponseDto.Data = flowStepDtos;
            dataTableResponseDto.TotalRecords = flowStepDtos.Count;

            return ResultDto<LazyResponseDto<FlowStepDto>>.Success(dataTableResponseDto);
        }
    }
}
