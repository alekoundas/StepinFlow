using AutoMapper;
using Core.Models.Database;
using Core.Models.Dtos;
using Core.Models.Ipc;
using DataAccess;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Business.Ipc.Handlers
{
    public class CreateFlowHandler : IRequestHandler<CreateFlowCommand, ResultDto<int>>
    {
        private readonly IMapper _mapper;
        private IDbContextFactory<AppDbContext> _dbContextFactory;

        public CreateFlowHandler(IMapper mapper, IDbContextFactory<AppDbContext> dbContextFactory)
        {
            _mapper = mapper;
            _dbContextFactory = dbContextFactory;
        }

        public async Task<ResultDto<int>> Handle(CreateFlowCommand request, CancellationToken ct)
        {
            await using AppDbContext dbContext = await _dbContextFactory.CreateDbContextAsync(ct);

            Flow flow = _mapper.Map<Flow>(request.dto);
            flow.Id = 0;

            dbContext.Flows.Add(flow);
            await dbContext.SaveChangesAsync(ct);

            return ResultDto<int>.Success(flow.Id);
        }
    }
}
