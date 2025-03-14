using Business.DatabaseContext;
using Model.Models;
using System.Linq.Expressions;

namespace Business.Repository
{
    public interface IBaseRepository<TEntity> where TEntity : class
    {

        BaseRepository<TEntity> ClearQuery();

        BaseRepository<TEntity> SetDbContext(InMemoryDbContext context);
        BaseRepository<TEntity> Where(Expression<Func<TEntity, bool>> predicate);
        BaseRepository<TEntity> Include<TProperty>(Expression<Func<TEntity, TProperty>> navigationProperty);

        BaseRepository<Flow> Select<TResult>(Expression<Func<TEntity, Flow>> selector);
        BaseRepository<FlowStep> Select<TResult>(Expression<Func<TEntity, FlowStep>> selector);
        BaseRepository<FlowParameter> Select<TResult>(Expression<Func<TEntity, FlowParameter>> selector);
        BaseRepository<FlowStep> SelectMany<TResult>(Expression<Func<TEntity, IEnumerable<FlowStep>>> selector);
        BaseRepository<FlowParameter> SelectMany<TResult>(Expression<Func<TEntity, IEnumerable<FlowParameter>>> selector);


        BaseRepository<TEntity> OrderBy<TKey>(Expression<Func<TEntity, TKey>> keySelector);


        int Count();
        Task<int> CountAsync();
      
        bool Any(Expression<Func<TEntity, bool>> predicate);
        Task<bool> AnyAsync(Expression<Func<TEntity, bool>> predicate);


        void Add(TEntity entity);
        Task AddAsync(TEntity entity);
        void AddRange(IEnumerable<TEntity> entities);
        Task AddRangeAsync(IEnumerable<TEntity> entities);


        void Remove(TEntity entity);
        Task RemoveAsync(TEntity entity);

        void RemoveRange(IEnumerable<TEntity> entities);
        Task RemoveRangeAsync(IEnumerable<TEntity> entities);


        Task<TEntity> FirstAsync(Expression<Func<TEntity, bool>> filter);
        TEntity? FirstOrDefault(Expression<Func<TEntity, bool>>? predicate);
        Task<TEntity?> FirstOrDefaultAsync(Expression<Func<TEntity, bool>>? predicate);

        List<TEntity> ToList();
        Task<List<TEntity>> ToListAsync();

        void Dispose();
    }
}
