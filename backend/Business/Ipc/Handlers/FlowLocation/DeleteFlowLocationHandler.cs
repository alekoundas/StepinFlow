using Core.Models.Dtos;
using Core.Models.Ipc;
using DataAccess;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Business.Ipc.Handlers
{
    public class DeleteFlowLocationHandler : IRequestHandler<DeleteFlowLocationCommand, ResultDto<bool>>
    {
        private IDbContextFactory<AppDbContext> _dbContextFactory;

        public DeleteFlowLocationHandler(IDbContextFactory<AppDbContext> dbContextFactory)
        {
            _dbContextFactory = dbContextFactory;
        }

        public async Task<ResultDto<bool>> Handle(DeleteFlowLocationCommand request, CancellationToken ct)
        {
            await using AppDbContext dbContext = await _dbContextFactory.CreateDbContextAsync(ct);

            int count = await dbContext.FlowLocations
                .Where(x => x.Id == request.id)
                .ExecuteDeleteAsync(ct);

            if (count <= 0)
                return ResultDto<bool>.Failure("Entity doesnt exist in the Database!");

            return ResultDto<bool>.Success(true);
        }
    }
}
