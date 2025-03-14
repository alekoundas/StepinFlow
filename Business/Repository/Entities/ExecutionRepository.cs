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
    }
}
