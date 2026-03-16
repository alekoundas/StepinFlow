namespace Business.DataService.Services
{
    public interface IDataService
    {
        Task<int> AddAsync<TEntity>(TEntity model) where TEntity : class;
        Task AddRangeAsync<TEntity>(List<TEntity> models) where TEntity : class;


        Task<int> UpdateAsync<TEntity>(TEntity model) where TEntity : class;
        Task<int> UpdateRangeAsync<TEntity>(List<TEntity> models) where TEntity : class;


        Task<int> DeleteByIdAsync<TEntity>(int id) where TEntity : class;
        Task<int> DeleteAsync<TEntity>(TEntity model) where TEntity : class;
        Task<int> DeleteRangeAsync<TEntity>(List<TEntity> models) where TEntity : class;
    }
}
