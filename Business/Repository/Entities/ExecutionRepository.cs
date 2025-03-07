using Business.DatabaseContext;
using Business.Repository.Interfaces;
using Microsoft.EntityFrameworkCore;
using Model.Models;

namespace Business.Repository.Entities
{
    public class ExecutionRepository : BaseRepository<Execution>, IExecutionRepository
    {
        private readonly IDbContextFactory<InMemoryDbContext> _contextFactory;
        private InMemoryDbContext _dbContext { get => _contextFactory.CreateDbContext(); }

        public ExecutionRepository(IDbContextFactory<InMemoryDbContext> contextFactory) : base(contextFactory)
        {
            _contextFactory = contextFactory;
        }


        public async Task<List<Execution>> GetAllParentLoopExecutions(int executionId)
        {
            var context = _contextFactory.CreateDbContext();
            List<Execution> parentLoopExecutions = new List<Execution>();
            Execution? currentExecution = await _dbContext.Executions
                .AsNoTracking()
                .Include(x => x.FlowStep)
                .FirstAsync(x => x.Id == executionId);

            while (currentExecution.ParentLoopExecutionId != null)
            {
                parentLoopExecutions.Add(currentExecution);

                currentExecution = await context.Executions
                    .AsNoTracking()
                    .Include(x => x.FlowStep)
                    .FirstAsync(x => x.Id == currentExecution.ParentLoopExecutionId.Value);
            }

            return parentLoopExecutions;
        }
    }
}
