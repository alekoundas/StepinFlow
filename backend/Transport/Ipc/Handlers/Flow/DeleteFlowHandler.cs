using Core.Models.Dtos;
using Core.Models.Ipc;
using DataAccess;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Transport.Ipc.Handlers
{
    public class DeleteFlowHandler : IRequestHandler<DeleteFlowCommand, ResultDto<bool>>
    {
        private readonly IDbContextFactory<AppDbContext> _dbContextFactory;

        public DeleteFlowHandler(IDbContextFactory<AppDbContext> dbContextFactory)
        {
            _dbContextFactory = dbContextFactory;
        }

        public async Task<ResultDto<bool>> Handle(DeleteFlowCommand request, CancellationToken ct)
        {
            await using AppDbContext dbContext = await _dbContextFactory.CreateDbContextAsync(ct);

            int count = await dbContext.Flows
                .Where(x => x.Id == request.Id)
                .ExecuteDeleteAsync(ct);

            if (count <= 0)
                return ResultDto<bool>.Failure("Entity doesnt exist in the Database!");

            return ResultDto<bool>.Success(true);
        }
    }
}
