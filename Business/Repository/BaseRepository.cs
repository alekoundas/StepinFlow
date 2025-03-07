using Business.DatabaseContext;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;
using System;
using System.Linq.Expressions;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

namespace Business.Repository
{
    public class BaseRepository<TEntity> : IBaseRepository<TEntity> where TEntity : class
    {
        private readonly IDbContextFactory<InMemoryDbContext> _contextFactory;
        private InMemoryDbContext? _context;
        private bool _ownsContext = true;
        private IQueryable<TEntity>? _query;

        public BaseRepository(IDbContextFactory<InMemoryDbContext> contextFactory)
        {
            _contextFactory = contextFactory;
        }

        //protected DbSet<TEntity> GetDbSet() => _context?.Set<TEntity>() ?? _contextFactory.CreateDbContext().Set<TEntity>();
        protected InMemoryDbContext GetDbContext()
        {
            if (_context == null)
                _context = _contextFactory.CreateDbContext();

            return _context;
        }


        public BaseRepository<TEntity> SetDbContext(InMemoryDbContext context)
        {
            if (_context != null && _ownsContext)
                _context.Dispose();

            _context = context;
            _ownsContext = false;
            return this;
        }

        public BaseRepository<TEntity> Where(Expression<Func<TEntity, bool>> predicate)
        {
            if (_query == null)
                _query = GetDbContext().Set<TEntity>();

            _query = _query.Where(predicate);
            return this;
        }

        public BaseRepository<TEntity> Include<TProperty>(Expression<Func<TEntity, TProperty>> navigationProperty)
        {
            if (_query == null)
                _query = GetDbContext().Set<TEntity>();

            _query = _query.Include(navigationProperty);
            return this;
        }

        //        public BaseRepository<TEntity> ThenInclude<TPreviousProperty, TProperty>(
        //Expression<Func<TPreviousProperty, TProperty>> navigationProperty)
        //        {
        //            if (_query is IIncludableQueryable<TEntity, TPreviousProperty> includableQuery)
        //            {
        //                _query = includableQuery.ThenInclude(navigationProperty);
        //            }
        //            else
        //            {
        //                throw new InvalidOperationException("ThenInclude must be called after Include.");
        //            }
        //            return this;
        //        }



        public IQueryable<TEntity> Query { get => _contextFactory.CreateDbContext().Set<TEntity>(); }




        public List<TEntity> GetAll()
        {
            var result = GetDbContext().Set<TEntity>().ToList();
            Dispose();
            return result;
        }

        public async Task<List<TEntity>> GetAllAsync()
        {
            var result = await GetDbContext().Set<TEntity>().ToListAsync();
            Dispose();
            return result;
        }

        //public async Task<List<TResult>> SelectAllAsync<TResult>(Expression<Func<TEntity, TResult>> selector)
        //{
        //    using var context = _contextFactory.CreateDbContext();
        //    return await context.Set<TEntity>().Select(selector).ToListAsync();
        //}

        //public async Task<List<TResult>> SelectAllAsyncFiltered<TResult>(Expression<Func<TEntity, bool>> predicate, Expression<Func<TEntity, TResult>> selector)
        //{
        //    using var context = _contextFactory.CreateDbContext();
        //    return await context.Set<TEntity>().Where(predicate).Select(selector).ToListAsync();
        //}


        public int Count()
        {
            var result = GetDbContext().Set<TEntity>().Count();
            Dispose();
            return result;
        }
        public async Task<int> CountAsync()
        {
            var result = await GetDbContext().Set<TEntity>().CountAsync();
            Dispose();
            return result;
        }

        //public async Task<int> CountAllAsyncFiltered(Expression<Func<TEntity, bool>> selector)
        //{
        //    using var context = _contextFactory.CreateDbContext();
        //    return await context.Set<TEntity>().Where(selector).CountAsync();
        //}


        //public async Task<List<TEntity>> GetPaggingWithFilter(
        //    Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>> orderingInfo,
        //    Expression<Func<TEntity, bool>>? filter,
        //    List<Func<IQueryable<TEntity>, IIncludableQueryable<TEntity, object>>>? includes = null,
        //    int pageSize = 10,
        //    int pageIndex = 1)
        //{
        //    using var context = _contextFactory.CreateDbContext();
        //    var qry = context.Set<TEntity>().AsQueryable();

        //    if (includes != null)
        //        foreach (var include in includes)
        //            qry = include(qry);

        //    if (filter != null)
        //        qry = qry.Where(filter);

        //    if (orderingInfo != null)
        //        qry = orderingInfo(qry);

        //    if (pageSize != -1 && pageSize != 0)
        //        qry = qry.Skip((pageIndex - 1) * pageSize).Take(pageSize);

        //    return await qry.ToListAsync();
        //}

        //public async Task<List<TEntity>> GetWithFilter(
        //    Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>> orderingInfo,
        //    Expression<Func<TEntity, bool>>? filter,
        //    List<Func<IQueryable<TEntity>, IIncludableQueryable<TEntity, object>>>? includes = null)
        //{
        //    using var context = _contextFactory.CreateDbContext();
        //    var qry = context.Set<TEntity>().AsQueryable();

        //    if (includes != null)
        //        foreach (var include in includes)
        //            qry = include(qry);

        //    if (filter != null)
        //        qry = qry.Where(filter);

        //    if (orderingInfo != null)
        //        qry = orderingInfo(qry);

        //    return await qry.ToListAsync();
        //}

        //public IQueryable<TEntity> GetWithFilterQueryable(
        //    Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>> orderingInfo,
        //    Expression<Func<TEntity, bool>>? filter,
        //    List<Func<IQueryable<TEntity>, IIncludableQueryable<TEntity, object>>>? includes = null,
        //    int pageSize = 10,
        //    int pageIndex = 1)
        //{
        //    var context = _contextFactory.CreateDbContext(); // Note: Caller must dispose
        //    var qry = context.Set<TEntity>().AsQueryable();

        //    if (includes != null)
        //        foreach (var include in includes)
        //            qry = include(qry);

        //    if (filter != null)
        //        qry = qry.Where(filter);

        //    if (orderingInfo != null)
        //        qry = orderingInfo(qry);

        //    return qry; 
        //}



        public bool Any(Expression<Func<TEntity, bool>> predicate)
        {
            var result = GetDbContext().Set<TEntity>().Any(predicate);
            Dispose();
            return result;
        }

        public async Task<bool> AnyAsync(Expression<Func<TEntity, bool>> predicate)
        {
            var result = await GetDbContext().Set<TEntity>().AnyAsync(predicate);
            Dispose();
            return result;
        }

        public void Add(TEntity entity)
        {
            var context = GetDbContext();
            context.Set<TEntity>().Add(entity);
            context.SaveChanges();
            Dispose();
        }

        public async Task AddAsync(TEntity entity)
        {
            await GetDbContext().AddAsync(entity);
            await GetDbContext().SaveChangesAsync();
            Dispose();
        }

        //public void Select(Expression<Func<TEntity, bool>> predicate)
        //{
        //    using var context = _contextFactory.CreateDbContext();
        //    context.Set<TEntity>().Select(predicate); // Note: This doesn't persist; likely a mistake
        //}

        //public void Select(Func<TEntity, TEntity> predicate)
        //{
        //    using var context = _contextFactory.CreateDbContext();
        //    context.Set<TEntity>().Select(predicate); // Note: This doesn't persist; likely a mistake
        //}

        public void AddRange(IEnumerable<TEntity> entities)
        {
            var context = GetDbContext();
            context.Set<TEntity>().AddRange(entities);
            context.SaveChanges();
            Dispose();
        }
        public async Task AddRangeAsync(IEnumerable<TEntity> entities)
        {
            var context = GetDbContext();
            await context.Set<TEntity>().AddRangeAsync(entities);
            await context.SaveChangesAsync();
            Dispose();
        }

        public void Remove(TEntity entity)
        {
            var context = GetDbContext();
            context.Set<TEntity>().Remove(entity);
            context.SaveChanges();
            Dispose();
        }
        public async Task RemoveAsync(TEntity entity)
        {
            var context = GetDbContext();
            context.Set<TEntity>().Remove(entity);
            await context.SaveChangesAsync();
            Dispose();
        }

        public void RemoveRange(IEnumerable<TEntity> entities)
        {
            var context = GetDbContext();
            context.Set<TEntity>().RemoveRange(entities);
            context.SaveChangesAsync();
            Dispose();
        }
        public async Task RemoveRangeAsync(IEnumerable<TEntity> entities)
        {
            var context = GetDbContext();
            context.Set<TEntity>().RemoveRange(entities);
            await context.SaveChangesAsync();
            Dispose();
        }

        //public async Task<TEntity?> FindAsync(int id)
        //{
        //    using var context = _contextFactory.CreateDbContext();
        //    return await context.Set<TEntity>().FindAsync(id);
        //}

        public async Task<TEntity> FirstAsync(Expression<Func<TEntity, bool>> filter)
        {
            TEntity? result = null;

            if (_query == null)
                result = await GetDbContext().Set<TEntity>().AsNoTracking().FirstAsync(filter);
            else
                result = await _query.AsNoTracking().FirstAsync(filter);

            Dispose();
            return result;
        }


        public TEntity? FirstOrDefault(Expression<Func<TEntity, bool>> filter)
        {
            TEntity? result = null;

            if (_query == null)
                result = GetDbContext().Set<TEntity>().AsNoTracking().FirstOrDefault(filter);
            else
                result = _query.AsNoTracking().FirstOrDefault(filter);

            Dispose();
            return result;
        }

        public async Task<TEntity?> FirstOrDefaultAsync(Expression<Func<TEntity, bool>> filter)
        {
            TEntity? result = null;

            if (_query == null)
                result = await GetDbContext().Set<TEntity>().AsNoTracking().FirstOrDefaultAsync(filter);
            else
                result = await _query.AsNoTracking().FirstOrDefaultAsync(filter);
            Dispose();
            return result;
        }


        //public async Task<List<TEntity>> GetFiltered(Expression<Func<TEntity, bool>> filter)
        //{
        //    using var context = _contextFactory.CreateDbContext();
        //    return await context.Set<TEntity>().Where(filter).ToListAsync();
        //}

        //public IEnumerable<TEntity> Where(Expression<Func<TEntity, bool>> expression)
        //{
        //    using var context = _contextFactory.CreateDbContext();
        //    return context.Set<TEntity>().Where(expression).ToList();
        //}


        public List<TEntity> ToList()
        {
            List<TEntity>? result = null;

            if (_query == null)
                result = GetDbContext().Set<TEntity>().AsNoTracking().ToList();
            else
                result = _query.AsNoTracking().ToList();


            Dispose();
            return result;
        }

        public async Task<List<TEntity>> ToListAsync()
        {
            List<TEntity>? result = null;

            if (_query == null)
                result = await GetDbContext().Set<TEntity>().AsNoTracking().ToListAsync();
            else
                result = await _query.AsNoTracking().ToListAsync();


            Dispose();
            return result;
        }




        public void Dispose()
        {
            if (_ownsContext && _context != null)
                _context.Dispose();

            _query = null;
            _context = null;
            _ownsContext = true;
        }
    }
}