using Core.Models.Dtos;
using DataAccess;
using Microsoft.EntityFrameworkCore;

namespace Transport.Ipc.Handlers
{
    public class DeleteFlowAreaHandler
    {
        private readonly IDbContextFactory<AppDbContext> _dbContextFactory;

        public DeleteFlowAreaHandler(IDbContextFactory<AppDbContext> dbContextFactory)
        {
            _dbContextFactory = dbContextFactory;
        }

        public async Task<ResultDto<bool>> HandleAsync(int id, CancellationToken ct)
        {
            await using AppDbContext dbContext = await _dbContextFactory.CreateDbContextAsync(ct);

            int count = await dbContext.FlowAreas
                .Where(x => x.Id == id)
                .ExecuteDeleteAsync(ct);

            if (count <= 0)
                return ResultDto<bool>.Failure("Entity doesnt exist in the Database!");

            return ResultDto<bool>.Success(true);
        }
    }
}
