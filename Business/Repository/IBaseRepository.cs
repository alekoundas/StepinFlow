using Business.DatabaseContext;
using Microsoft.EntityFrameworkCore.Query;
using System.Linq.Expressions;

namespace Business.Repository
{
    public interface IBaseRepository<TEntity> where TEntity : class
    {


        //BaseRepository<TEntity> Expression();
        //BaseRepository<TEntity> Where(Expression<Func<TEntity, bool>> predicate);
        //BaseRepository<TEntity> Include<TProperty>(Expression<Func<TEntity, TProperty>> navigationProperty);
        //BaseRepository<TEntity> ThenInclude<TPreviousProperty, TProperty>(Expression<Func<TPreviousProperty, TProperty>> navigationProperty);
        //BaseRepository<TEntity> ThenInclude<TPreviousProperty, TProperty>(Expression<Func<TPreviousProperty, TProperty>> navigationProperty);


        IQueryable<TEntity> Query { get; }
        BaseRepository<TEntity> SetDbContext(InMemoryDbContext context);
        BaseRepository<TEntity> Where(Expression<Func<TEntity, bool>> predicate);
        BaseRepository<TEntity> Include<TProperty>(Expression<Func<TEntity, TProperty>> navigationProperty);



        //Excell reflection call
        List<TEntity> GetAll();
        Task<List<TEntity>> GetAllAsync();

        int Count();
        Task<int> CountAsync();
        //Task<int> CountAllAsyncFiltered(Expression<Func<TEntity, bool>> filter);
        //Task<List<TResult>> SelectAllAsync<TResult>(Expression<Func<TEntity, TResult>> selector);
        //Task<List<TResult>> SelectAllAsyncFiltered<TResult>(Expression<Func<TEntity, bool>> predicate, Expression<Func<TEntity, TResult>> selector);
        //Task<TEntity?> FindAsync(int id);

        //Task<List<TEntity>> GetPaggingWithFilter(
        //    Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>> orderingInfo,
        //    Expression<Func<TEntity, bool>>? filter = null,
        //    List<Func<IQueryable<TEntity>, IIncludableQueryable<TEntity, object>>>? includes = null,
        //    int pageSize = 10,
        //    int pageIndex = 1);
        //Task<List<TEntity>> GetWithFilter(
        //   Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>> orderingInfo,
        //   Expression<Func<TEntity, bool>>? filter = null,
        //   List<Func<IQueryable<TEntity>, IIncludableQueryable<TEntity, object>>>? includes = null);
        //IQueryable<TEntity> GetWithFilterQueryable(
        // Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>> orderingInfo,
        //   Expression<Func<TEntity, bool>>? filter = null,
        //   List<Func<IQueryable<TEntity>, IIncludableQueryable<TEntity, object>>>? includes = null,
        //   int pageSize = 10,
        //   int pageIndex = 1);

        //Task<List<TEntity>> GetFiltered(Expression<Func<TEntity, bool>> filter);

        //int Count(Expression<Func<TEntity, bool>> predicate);
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

        //IEnumerable<TEntity> Where(Expression<Func<TEntity, bool>> expression);

        Task<TEntity> FirstAsync(Expression<Func<TEntity, bool>> filter);
        TEntity? FirstOrDefault(Expression<Func<TEntity, bool>> predicate);
        Task<TEntity?> FirstOrDefaultAsync(Expression<Func<TEntity, bool>> predicate);

        List<TEntity> ToList();
        Task<List<TEntity>> ToListAsync();

        //void Select(Func<TEntity, TEntity> predicate);
        //void Select(Expression<Func<TEntity, bool>> predicate);
    }
}
