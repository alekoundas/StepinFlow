using AutoMapper;
using Core.Models.Database;
using Core.Models.Dtos;
using DataAccess;
using Microsoft.EntityFrameworkCore;

namespace Transport.Ipc.Handlers
{
    public class UpdateFlowPointHandler
    {
        private readonly IMapper _mapper;
        private readonly IDbContextFactory<AppDbContext> _dbContextFactory;

        public UpdateFlowPointHandler(IMapper mapper, IDbContextFactory<AppDbContext> dbContextFactory)
        {
            _mapper = mapper;
            _dbContextFactory = dbContextFactory;
        }

        public async Task<ResultDto<FlowPointDto>> HandleAsync(FlowPointDto dto, CancellationToken ct)
        {
            await using AppDbContext dbContext = await _dbContextFactory.CreateDbContextAsync(ct);

            FlowPoint? existingFlowPoint = await dbContext.FlowPoints
                .FirstOrDefaultAsync(x => x.Id == dto.Id, ct);

            if (existingFlowPoint == null)
                return ResultDto<FlowPointDto>.Failure("Entity doesnt exist in the Database!");

            dbContext.Entry(existingFlowPoint).CurrentValues.SetValues(dto);

            await dbContext.SaveChangesAsync(ct);

            FlowPointDto flowPointDto = _mapper.Map<FlowPointDto>(existingFlowPoint);
            return ResultDto<FlowPointDto>.Success(flowPointDto);
        }
    }
}
