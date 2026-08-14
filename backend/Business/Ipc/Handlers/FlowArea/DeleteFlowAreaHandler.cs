using Core.Models.Dtos;
using Core.Models.Ipc;
using DataAccess;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Business.Ipc.Handlers
{
    public class DeleteFlowAreaHandler : IRequestHandler<DeleteFlowAreaCommand, ResultDto<bool>>
    {
        private IDbContextFactory<AppDbContext> _dbContextFactory;

        public DeleteFlowAreaHandler(IDbContextFactory<AppDbContext> dbContextFactory)
        {
            _dbContextFactory = dbContextFactory;
        }

        public async Task<ResultDto<bool>> Handle(DeleteFlowAreaCommand request, CancellationToken ct)
        {
            await using AppDbContext dbContext = await _dbContextFactory.CreateDbContextAsync(ct);

            int count = await dbContext.FlowAreas
                .Where(x => x.Id == request.id)
                .ExecuteDeleteAsync(ct);

            if (count <= 0)
                return ResultDto<bool>.Failure("Entity doesnt exist in the Database!");

            return ResultDto<bool>.Success(true);
        }
    }
}
