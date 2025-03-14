using Business.DatabaseContext;
using Business.Repository.Interfaces;
using Business.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Model.Models;

namespace Business.Services
{
    public class DataService : IDataService
    {
        private readonly IDbContextFactory<InMemoryDbContext> _contextFactory;
        public InMemoryDbContext CreateNewDbContext { get => _contextFactory.CreateDbContext(); }
        private InMemoryDbContext? _dbContext { get; set; }
        private bool _ownsContext = true;

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


            Flows = flowRepository;
            FlowSteps = flowStepRepository;
            FlowParameters = flowParameterRepository;
            Executions = executionRepository;
            AppSettings = appSettingRepository;
        }

        public void SetDbContext(InMemoryDbContext dbContext)
        {
            if (_dbContext != null && _ownsContext)
                _dbContext.Dispose();

            _ownsContext = false;
            _dbContext = dbContext;
            Flows.SetDbContext(dbContext);
            FlowSteps.SetDbContext(dbContext);
            FlowParameters.SetDbContext(dbContext);
            Executions.SetDbContext(dbContext);
            AppSettings.SetDbContext(dbContext);

        }
        private InMemoryDbContext GetDbContext()
        {
            //if (_dbContext == null)
                _dbContext = _contextFactory.CreateDbContext();
            _dbContext.Database.ExecuteSqlRaw("PRAGMA busy_timeout = 10000;");
            return _dbContext;
        }



        public void Update<TEntity>(TEntity model)
        {
            var context = GetDbContext();
            if (model == null)
                return;

                context.Entry(model).State = EntityState.Modified;
            context.SaveChanges();
            Dispose();
        }

        public async Task UpdateAsync<TEntity>(TEntity model)
        {
            var context = GetDbContext();
            if (model == null) 
                return;
            context.Entry(model).State = EntityState.Modified;
            await context.SaveChangesAsync();
            Dispose();
        }


        public void UpdateRange<TEntity>(List<TEntity> models)
        {
            var context = GetDbContext();
            foreach (var model in models)
                if (model != null)
                    context.Entry(model).State = EntityState.Modified;

            context.SaveChanges();

            Dispose();
        }

        public async Task UpdateRangeAsync<TEntity>(List<TEntity> models)
        {
            var context = GetDbContext();
            foreach (var model in models)
                if (model != null)
                        context.Entry(model).State = EntityState.Modified;

            await context   .SaveChangesAsync();

            Dispose();
        }

        //public void ClearChangeTracker()
        //{
        //    GetDbContext().ChangeTracker.Clear();
        //}

        public void Dispose(bool forceDispose = false)
        {
            //if (forceDispose)
            //{
            //    if (_dbContext != null)
            //        _dbContext.Dispose();
            //    _dbContext = null;
            //}

            //if (_ownsContext && _dbContext != null)
            //{
            //    _dbContext.Dispose();
            //    _dbContext = null;
            //}

            //Flows.Dispose(forceDispose);
            //FlowSteps.Dispose(forceDispose);
            //FlowParameters.Dispose(forceDispose);
            //Executions.Dispose(forceDispose);
            //AppSettings.Dispose(forceDispose);
            _dbContext = null;
        }
    }
}
