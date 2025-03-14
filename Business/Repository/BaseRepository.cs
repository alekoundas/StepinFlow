using Business.DatabaseContext;
using Microsoft.EntityFrameworkCore;
using Model.Models;
using System.Linq.Expressions;

namespace Business.Repository
{
    public class BaseRepository<TEntity> : IBaseRepository<TEntity> where TEntity : class
    {
        private readonly IDbContextFactory<InMemoryDbContext> _contextFactory;
        protected InMemoryDbContext? _context;
        protected IQueryable<TEntity>? _query;

        public BaseRepository(IDbContextFactory<InMemoryDbContext> contextFactory)
        {
            _contextFactory = contextFactory;
        }

        protected InMemoryDbContext GetDbContext()
        {
                _context = _contextFactory.CreateDbContext();
            _context.Database.ExecuteSqlRaw("PRAGMA busy_timeout = 10000;");

            return _context;
        }

        //TODO remove this
        public BaseRepository<TEntity> SetDbContext(InMemoryDbContext context)
        {
            return this;
        }

        public BaseRepository<TEntity> ClearQuery()
        {
            _query = null;
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

        //TODO: Find a way to combine Select methods.
        public BaseRepository<FlowStep> Select<TResult>(Expression<Func<TEntity, FlowStep>> selector)
        {
            if (_query == null)
                _query = _context.Set<TEntity>();

            var newQuery = _query.Select(selector);
            return new BaseRepository<FlowStep>(_contextFactory)
            {
                _context = _context,
                _query = newQuery
            };
        }
        public BaseRepository<Flow> Select<TResult>(Expression<Func<TEntity, Flow>> selector)
        {
            if (_query == null)
                _query = _context.Set<TEntity>();

            var newQuery = _query.Select(selector);
            return new BaseRepository<Flow>(_contextFactory)
            {
                _context = _context,
                _query = newQuery
            };
        }
        public BaseRepository<FlowParameter> Select<TResult>(Expression<Func<TEntity, FlowParameter>> selector)
        {
            if (_query == null)
                _query = _context.Set<TEntity>();

            var newQuery = _query.Select(selector);
            return new BaseRepository<FlowParameter>(_contextFactory)
            {
                _context = _context,
                _query = newQuery
            };
        }


        public BaseRepository<FlowStep> SelectMany<TResult>(Expression<Func<TEntity, IEnumerable<FlowStep>>> selector)
        {
            if (_query == null)
                _query = _context.Set<TEntity>();

            var newQuery = _query.SelectMany(selector);
            return new BaseRepository<FlowStep>(_contextFactory)
            {
                _context = _context,
                _query = newQuery
            };
        }

        public BaseRepository<FlowParameter> SelectMany<TResult>(Expression<Func<TEntity, IEnumerable<FlowParameter>>> selector)
        {
            if (_query == null)
                _query = _context.Set<TEntity>();

            var newQuery = _query.SelectMany(selector);
            return new BaseRepository<FlowParameter>(_contextFactory)
            {
                _context = _context,
                _query = newQuery
            };
        }





        public BaseRepository<TEntity> OrderBy<TKey>(Expression<Func<TEntity, TKey>> keySelector)
        {
            if (_query == null)
                _query = GetDbContext().Set<TEntity>();

            _query = _query.OrderBy(keySelector);

            return this;
        }










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
            var context = GetDbContext();
            await context.AddAsync(entity);
            await context.SaveChangesAsync();
            Dispose();
        }


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

            if (_query == null || _context == null || _context.Database.GetDbConnection() == null)
                result = await GetDbContext().Set<TEntity>().AsNoTracking().FirstAsync(filter);
            else
                result = await _query.AsNoTracking().FirstAsync(filter);

            Dispose();
            return result;
        }


        public TEntity? FirstOrDefault(Expression<Func<TEntity, bool>>? filter = null)
        {
            TEntity? result = null;
            if (filter != null)
            {
                if (_query == null || _context == null || _context.Database.GetDbConnection() == null)
                    result = GetDbContext().Set<TEntity>().AsNoTracking().FirstOrDefault(filter);
                else
                    result = _query.AsNoTracking().FirstOrDefault(filter);
            }
            else
            {
                if (_query == null || _context == null || _context.Database.GetDbConnection() == null)
                    result = GetDbContext().Set<TEntity>().AsNoTracking().FirstOrDefault();
                else
                    result = _query.AsNoTracking().FirstOrDefault();
            }

            Dispose();
            return result;
        }

        public async Task<TEntity?> FirstOrDefaultAsync(Expression<Func<TEntity, bool>>? filter = null)
        {
            TEntity? result = null;
            if (filter != null)
            {
                if (_query == null || _context == null || _context.Database.GetDbConnection() == null)
                    result = await GetDbContext().Set<TEntity>().AsNoTracking().FirstOrDefaultAsync(filter);
                else
                    result = await _query.AsNoTracking().FirstOrDefaultAsync(filter);
            }
            else
            {
                if (_query == null || _context == null || _context.Database.GetDbConnection() == null)
                    result = await GetDbContext().Set<TEntity>().AsNoTracking().FirstOrDefaultAsync();
                else
                    result = await _query.AsNoTracking().FirstOrDefaultAsync();
            }
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

            if (_query == null || _context == null || _context.Database.GetDbConnection() == null)
                result = GetDbContext().Set<TEntity>().AsNoTracking().ToList();
            else
                result = _query.AsNoTracking().ToList();


            Dispose();
            return result;
        }

        public async Task<List<TEntity>> ToListAsync()
        {
            List<TEntity>? result = null;

            if (_query == null || _context == null || _context.Database.GetDbConnection() == null)
                result = await GetDbContext().Set<TEntity>().AsNoTracking().ToListAsync();
            else
                result = await _query.AsNoTracking().ToListAsync();


            Dispose();
            return result;
        }

      


        public void Dispose()
        {
            //if (forceDispose)
            //{
            //    if (_context != null)
            //        _context.Dispose();
            //    _context = null;
            //}

            //if (_ownsContext && _context != null)
            //{
            //    _context.Dispose();
            //    _context = null;
            //}

            _context = null;
            _query = null;
        }
    }
}