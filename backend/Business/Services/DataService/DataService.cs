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
        public async Task<int> AddAsync<TEntity>(TEntity model) where TEntity : class
        {
            await using AppDbContext dbContext = await _dbContextFactory.CreateDbContextAsync();
            dbContext.Set<TEntity>().Add(model);
            return await dbContext.SaveChangesAsync();
        }

        public async Task AddRangeAsync<TEntity>(List<TEntity> models) where TEntity : class
        {
            await using AppDbContext dbContext = await _dbContextFactory.CreateDbContextAsync();
            dbContext.Set<TEntity>().AddRange(models);
            await dbContext.SaveChangesAsync();
        }
     

        // Update
        public async Task<int> UpdateAsync<TEntity>(TEntity model) where TEntity : class
        {
            await using AppDbContext dbContext = await _dbContextFactory.CreateDbContextAsync();
            dbContext.Set<TEntity>().Update(model);
            return await dbContext.SaveChangesAsync();
        }

        public async Task<int> UpdateRangeAsync<TEntity>(List<TEntity> models) where TEntity : class
        {
            await using AppDbContext dbContext = await _dbContextFactory.CreateDbContextAsync();
            dbContext.Set<TEntity>().UpdateRange(models);
            return await dbContext.SaveChangesAsync();
        }


        // Delete
        public async Task<int> DeleteAsync<TEntity>(TEntity model) where TEntity : class
        {
            await using AppDbContext dbContext = await _dbContextFactory.CreateDbContextAsync();
            dbContext.Set<TEntity>().Remove(model);
            return await dbContext.SaveChangesAsync();
        }
        public async Task<int> DeleteRangeAsync<TEntity>(List<TEntity> models) where TEntity : class
        {
            await using AppDbContext dbContext = await _dbContextFactory.CreateDbContextAsync();
            dbContext.Set<TEntity>().RemoveRange(models);
            return await dbContext.SaveChangesAsync();
        }
        public async Task<int> DeleteByIdAsync<TEntity>(int id) where TEntity : class
        {
            await using AppDbContext dbContext = await _dbContextFactory.CreateDbContextAsync();
            TEntity? entity = await dbContext.Set<TEntity>().FindAsync(id);
            if (entity != null)
            {
                dbContext.Set<TEntity>().Remove(entity);
                return await dbContext.SaveChangesAsync();
            }
            return -1;
        }
    }
}
