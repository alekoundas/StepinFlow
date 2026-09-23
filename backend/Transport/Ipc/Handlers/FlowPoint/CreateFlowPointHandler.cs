using AutoMapper;
using Core.Models.Database;
using Core.Models.Dtos;
using DataAccess;
using Microsoft.EntityFrameworkCore;

namespace Transport.Ipc.Handlers
{
    public class CreateFlowPointHandler
    {
        private readonly IMapper _mapper;
        private readonly IDbContextFactory<AppDbContext> _dbContextFactory;

        public CreateFlowPointHandler(IMapper mapper, IDbContextFactory<AppDbContext> dbContextFactory)
        {
            _mapper = mapper;
            _dbContextFactory = dbContextFactory;
        }

        public async Task<ResultDto<int>> HandleAsync(FlowPointDto dto, CancellationToken ct)
        {
            await using AppDbContext dbContext = await _dbContextFactory.CreateDbContextAsync(ct);

            FlowPoint flowPoint = _mapper.Map<FlowPoint>(dto);
            flowPoint.Id = 0;

            dbContext.FlowPoints.Add(flowPoint);
            await dbContext.SaveChangesAsync(ct);

            return ResultDto<int>.Success(flowPoint.Id);
        }
    }
}
