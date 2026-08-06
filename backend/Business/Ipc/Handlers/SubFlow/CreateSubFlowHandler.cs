using AutoMapper;
using Core.Models.Database;
using Core.Models.Dtos;
using Core.Models.Ipc;
using DataAccess;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Business.Ipc.Handlers
{
    public class CreateSubFlowHandler : IRequestHandler<CreateSubFlowCommand, ResultDto<int>>
    {
        private readonly IMapper _mapper;
        private IDbContextFactory<AppDbContext> _dbContextFactory;

        public CreateSubFlowHandler(IMapper mapper, IDbContextFactory<AppDbContext> dbContextFactory)
        {
            _mapper = mapper;
            _dbContextFactory = dbContextFactory;
        }

        public async Task<ResultDto<int>> Handle(CreateSubFlowCommand request, CancellationToken ct)
        {
            await using AppDbContext dbContext = await _dbContextFactory.CreateDbContextAsync(ct);

            SubFlow subFlow = _mapper.Map<SubFlow>(request.dto);
            subFlow.Id = 0;

            dbContext.SubFlows.Add(subFlow);
            await dbContext.SaveChangesAsync(ct);

            return ResultDto<int>.Success(subFlow.Id);
        }
    }
}
