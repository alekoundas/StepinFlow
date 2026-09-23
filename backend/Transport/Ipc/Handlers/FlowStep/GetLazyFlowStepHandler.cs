using AutoMapper;
using Core.Models.Database;
using Core.Models.Dtos;
using DataAccess;
using Microsoft.EntityFrameworkCore;

namespace Transport.Ipc.Handlers
{
    public class GetLazyFlowStepHandler
    {
        private readonly IMapper _mapper;
        private readonly IDbContextFactory<AppDbContext> _dbContextFactory;

        public GetLazyFlowStepHandler(IMapper mapper, IDbContextFactory<AppDbContext> dbContextFactory)
        {
            _mapper = mapper;
            _dbContextFactory = dbContextFactory;
        }

        public async Task<ResultDto<LazyResponseDto<FlowStepDto>>> HandleAsync(LazyRequestDto dto, CancellationToken ct)
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
