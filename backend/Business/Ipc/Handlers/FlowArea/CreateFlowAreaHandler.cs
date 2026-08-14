using AutoMapper;
using Core.Models.Database;
using Core.Models.Dtos;
using Core.Models.Ipc;
using DataAccess;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Business.Ipc.Handlers
{
    public class CreateFlowAreaHandler : IRequestHandler<CreateFlowAreaCommand, ResultDto<int>>
    {
        private readonly IMapper _mapper;
        private IDbContextFactory<AppDbContext> _dbContextFactory;

        public CreateFlowAreaHandler(IMapper mapper, IDbContextFactory<AppDbContext> dbContextFactory)
        {
            _mapper = mapper;
            _dbContextFactory = dbContextFactory;
        }

        public async Task<ResultDto<int>> Handle(CreateFlowAreaCommand request, CancellationToken ct)
        {
            await using AppDbContext dbContext = await _dbContextFactory.CreateDbContextAsync(ct);

            FlowArea flowArea = _mapper.Map<FlowArea>(request.dto);
            flowArea.Id = 0;

            dbContext.FlowAreas.Add(flowArea);
            await dbContext.SaveChangesAsync(ct);

            return ResultDto<int>.Success(flowArea.Id);
        }
    }
}
