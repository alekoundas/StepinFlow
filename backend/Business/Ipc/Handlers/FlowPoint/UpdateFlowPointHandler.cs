using AutoMapper;
using Core.Models.Database;
using Core.Models.Dtos;
using Core.Models.Ipc;
using DataAccess;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Business.Ipc.Handlers
{
    public class UpdateFlowPointHandler : IRequestHandler<UpdateFlowPointCommand, ResultDto<FlowPointDto>>
    {
        private readonly IMapper _mapper;
        private IDbContextFactory<AppDbContext> _dbContextFactory;

        public UpdateFlowPointHandler(IMapper mapper, IDbContextFactory<AppDbContext> dbContextFactory)
        {
            _mapper = mapper;
            _dbContextFactory = dbContextFactory;
        }

        public async Task<ResultDto<FlowPointDto>> Handle(UpdateFlowPointCommand request, CancellationToken ct)
        {
            await using AppDbContext dbContext = await _dbContextFactory.CreateDbContextAsync(ct);

            FlowPoint? existingFlowPoint = await dbContext.FlowPoints
                .FirstOrDefaultAsync(x => x.Id == request.dto.Id, ct);

            if (existingFlowPoint == null)
                return ResultDto<FlowPointDto>.Failure("Entity doesnt exist in the Database!");

            dbContext.Entry(existingFlowPoint).CurrentValues.SetValues(request.dto);

            await dbContext.SaveChangesAsync(ct);

            FlowPointDto flowPointDto = _mapper.Map<FlowPointDto>(existingFlowPoint);
            return ResultDto<FlowPointDto>.Success(flowPointDto);
        }
    }
}
