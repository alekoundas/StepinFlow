using Business.DatabaseContext;
using Business.Repository.Interfaces;

namespace Business.Services.Interfaces
{
    public interface IExecutionDataService : IDisposable
    {
        InMemoryDbContext Query { get; }
        void SetDbContext(InMemoryDbContext dbContext);



        IFlowRepository Flows { get; set; }
        IFlowStepRepository FlowSteps { get; set; }
        IFlowParameterRepository FlowParameters { get; set; }
        IExecutionRepository Executions { get; set; }
        IAppSettingRepository AppSettings { get; set; }

        Task<int> SaveChangesAsync();
        int SaveChanges();
        void Update<TEntity>(TEntity model);
        void UpdateRange<TEntity>(List<TEntity> models);
        Task UpdateRangeAsync<TEntity>(List<TEntity> models);


        Task UpdateAsync<TEntity>(TEntity model);
    }
}
