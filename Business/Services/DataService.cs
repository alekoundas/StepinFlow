using Business.DatabaseContext;
using Business.Repository.Interfaces;
using Business.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Business.Services
{
    public class DataService : IDataService, IDisposable
    {
        private readonly IDbContextFactory<InMemoryDbContext> _contextFactory;
        public InMemoryDbContext Query { get => _contextFactory.CreateDbContext(); }
        //public InMemoryDbContext Query { get; set; }
        private InMemoryDbContext _dbContext { get; set; }

        public IFlowRepository Flows { get; set; }
        public IFlowStepRepository FlowSteps { get; set; }
        public IFlowParameterRepository FlowParameters { get; set; }
        public IExecutionRepository Executions { get; set; }
        public IAppSettingRepository AppSettings { get; set; }

        public DataService(
            IDbContextFactory<InMemoryDbContext> contextFactory,
            IFlowRepository flowRepository,
            IFlowStepRepository flowStepRepository,
            IFlowParameterRepository flowParameterRepository,
            IExecutionRepository executionRepository,
            IAppSettingRepository appSettingRepository)
        {
            _contextFactory = contextFactory;
            _dbContext = _contextFactory.CreateDbContext();
            //Query = _dbContext;


            Flows = flowRepository;
            FlowSteps = flowStepRepository;
            FlowParameters = flowParameterRepository;
            Executions = executionRepository;
            AppSettings = appSettingRepository;
        }

        public void SetDbContext(InMemoryDbContext dbContext)
        {
            _dbContext = dbContext;
            Flows.SetDbContext(dbContext);
            FlowSteps.SetDbContext(dbContext);
            FlowParameters.SetDbContext(dbContext);
            Executions.SetDbContext(dbContext);
            AppSettings.SetDbContext(dbContext);

            //Query = dbContext;
        }


        public async Task<int> SaveChangesAsync()
        {
            return await _dbContext.SaveChangesAsync();
        }

        public int SaveChanges()
        {
            return _dbContext.SaveChanges();
        }

        public void Update<TEntity>(TEntity model)
        {
            if (model == null)
                return;

            _dbContext.Entry(model).State = EntityState.Modified;
            _dbContext.SaveChanges();
        }

        public async Task UpdateAsync<TEntity>(TEntity model)
        {
            if (model == null)
                return;

            _dbContext.Entry(model).State = EntityState.Modified;
            await _dbContext.SaveChangesAsync();
        }


        public void UpdateRange<TEntity>(List<TEntity> models)
        {
            foreach (var model in models)
                if (model != null)
                    if (_dbContext.Entry(model).State == EntityState.Detached)
                        _dbContext.Entry(model).State = EntityState.Modified;
            _dbContext.SaveChanges();
        }

        public async Task UpdateRangeAsync<TEntity>(List<TEntity> models)
        {
            foreach (var model in models)
                if (model != null)
                    if (_dbContext.Entry(model).State == EntityState.Detached)
                        _dbContext.Entry(model).State = EntityState.Modified;

            await _dbContext.SaveChangesAsync();
        }

        public void Dispose()
        {
            _dbContext.Dispose();
        }
    }
}
