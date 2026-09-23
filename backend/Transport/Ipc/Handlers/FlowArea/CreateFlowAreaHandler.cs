using AutoMapper;
using Core.Models.Database;
using Core.Models.Dtos;
using DataAccess;
using Microsoft.EntityFrameworkCore;

namespace Transport.Ipc.Handlers
{
    public class CreateFlowAreaHandler
    {
        private readonly IMapper _mapper;
        private readonly    IDbContextFactory<AppDbContext> _dbContextFactory;

        public CreateFlowAreaHandler(IMapper mapper, IDbContextFactory<AppDbContext> dbContextFactory)
        {
            _mapper = mapper;
            _dbContextFactory = dbContextFactory;
        }

        public async Task<ResultDto<int>> HandleAsync(FlowAreaDto dto, CancellationToken ct)
        {
            await using AppDbContext dbContext = await _dbContextFactory.CreateDbContextAsync(ct);

            FlowArea flowArea = _mapper.Map<FlowArea>(dto);
            flowArea.Id = 0;

            dbContext.FlowAreas.Add(flowArea);
            await dbContext.SaveChangesAsync(ct);

            return ResultDto<int>.Success(flowArea.Id);
        }
    }
}
