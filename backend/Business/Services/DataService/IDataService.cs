namespace Business.DataService.Services
{
    public interface IDataService
    {
        Task AddAsync<TEntity>(TEntity model) where TEntity : class;
        Task AddRangeAsync<TEntity>(List<TEntity> models) where TEntity : class;


        Task UpdateAsync<TEntity>(TEntity model) where TEntity : class;
        Task UpdateRangeAsync<TEntity>(List<TEntity> models) where TEntity : class;


        Task DeleteByIdAsync<TEntity>(int id) where TEntity : class;
        Task DeleteAsync<TEntity>(TEntity model) where TEntity : class;
        Task DeleteRangeAsync<TEntity>(List<TEntity> models) where TEntity : class;
    }
}
