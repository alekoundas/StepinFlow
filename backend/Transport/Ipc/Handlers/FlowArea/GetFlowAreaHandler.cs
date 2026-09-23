using AutoMapper;
using Core.Models.Database;
using Core.Models.Dtos;
using DataAccess;
using Microsoft.EntityFrameworkCore;

namespace Transport.Ipc.Handlers
{
    public class GetFlowAreaHandler
    {
        private readonly IMapper _mapper;
        private readonly IDbContextFactory<AppDbContext> _dbContextFactory;

        public GetFlowAreaHandler(IMapper mapper, IDbContextFactory<AppDbContext> dbContextFactory)
        {
            _mapper = mapper;
            _dbContextFactory = dbContextFactory;
        }

        public async Task<ResultDto<FlowAreaDto>> HandleAsync(int id, CancellationToken ct)
        {
            await using AppDbContext dbContext = await _dbContextFactory.CreateDbContextAsync(ct);
            FlowArea? flowArea = await dbContext.FlowAreas
                .AsNoTracking()
                .Include(x => x.FlowSteps)
                .FirstOrDefaultAsync(x => x.Id == id, ct);

            if (flowArea == null)
                return ResultDto<FlowAreaDto>.Failure("Entity doesnt exist in the Database!");

            FlowAreaDto? flowAreaDto = _mapper.Map<FlowAreaDto>(flowArea);
            return ResultDto<FlowAreaDto>.Success(flowAreaDto);
        }
    }
}
