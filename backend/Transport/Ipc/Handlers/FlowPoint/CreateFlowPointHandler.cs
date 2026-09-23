using AutoMapper;
using Core.Models.Database;
using Core.Models.Dtos;
using Core.Models.Ipc;
using DataAccess;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Transport.Ipc.Handlers
{
    public class CreateFlowPointHandler : IRequestHandler<CreateFlowPointCommand, ResultDto<int>>
    {
        private readonly IMapper _mapper;
        private readonly IDbContextFactory<AppDbContext> _dbContextFactory;

        public CreateFlowPointHandler(IMapper mapper, IDbContextFactory<AppDbContext> dbContextFactory)
        {
            _mapper = mapper;
            _dbContextFactory = dbContextFactory;
        }

        public async Task<ResultDto<int>> Handle(CreateFlowPointCommand request, CancellationToken ct)
        {
            await using AppDbContext dbContext = await _dbContextFactory.CreateDbContextAsync(ct);

            FlowPoint flowPoint = _mapper.Map<FlowPoint>(request.Dto);
            flowPoint.Id = 0;

            dbContext.FlowPoints.Add(flowPoint);
            await dbContext.SaveChangesAsync(ct);

            return ResultDto<int>.Success(flowPoint.Id);
        }
    }
}
