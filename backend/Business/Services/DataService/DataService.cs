using DataAccess;
using Microsoft.EntityFrameworkCore;

namespace Business.DataService.Services
{
    public class DataService : IDataService
    {
        private IDbContextFactory<AppDbContext> _dbContextFactory;

        public DataService(IDbContextFactory<AppDbContext> dbContextFactory)
        {
            _dbContextFactory = dbContextFactory;
        }


        // Insert
        public async Task AddAsync<TEntity>(TEntity model) where TEntity : class
        {
            await using AppDbContext dbContext = await _dbContextFactory.CreateDbContextAsync();
            dbContext.Set<TEntity>().Add(model);
            await dbContext.SaveChangesAsync();
        }

        public async Task AddRangeAsync<TEntity>(List<TEntity> models) where TEntity : class
        {
            await using AppDbContext dbContext = await _dbContextFactory.CreateDbContextAsync();
            dbContext.Set<TEntity>().AddRange(models);
            await dbContext.SaveChangesAsync();
        }


        // Delete
        public async Task DeleteAsync<TEntity>(TEntity model) where TEntity : class
        {
            await using AppDbContext dbContext = await _dbContextFactory.CreateDbContextAsync();
            dbContext.Set<TEntity>().Remove(model);
            await dbContext.SaveChangesAsync();
        }

        public async Task DeleteRangeAsync<TEntity>(List<TEntity> models) where TEntity : class
        {
            await using AppDbContext dbContext = await _dbContextFactory.CreateDbContextAsync();
            dbContext.Set<TEntity>().RemoveRange(models);
            await dbContext.SaveChangesAsync();
        }


        // Update
        public async Task UpdateAsync<TEntity>(TEntity model) where TEntity : class
        {
            await using AppDbContext dbContext = await _dbContextFactory.CreateDbContextAsync();
            dbContext.Set<TEntity>().Update(model);
            await dbContext.SaveChangesAsync();
        }

        public async Task UpdateRangeAsync<TEntity>(List<TEntity> models) where TEntity : class
        {
            await using AppDbContext dbContext = await _dbContextFactory.CreateDbContextAsync();
            dbContext.Set<TEntity>().UpdateRange(models);
            await dbContext.SaveChangesAsync();
        }
        public async Task DeleteByIdAsync<TEntity>(int id) where TEntity : class
        {
            await using AppDbContext dbContext = await _dbContextFactory.CreateDbContextAsync();
            TEntity? entity = await dbContext.Set<TEntity>().FindAsync(id);
            if (entity == null)
            {
                dbContext.Set<TEntity>().Remove(entity);
                await dbContext.SaveChangesAsync();
            }
        }
    }
}
