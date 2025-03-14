using Business.DatabaseContext;
using Model.Models;

namespace Business.Factories
{
    public interface IExecutionWorker
    {
        void Initialize(Dictionary<int, List<Execution>> pendingExecutionLoops);
        void SetDbContext(InMemoryDbContext dbContext);



        Task<Execution> CreateExecutionModel(FlowStep flowStep, Execution parentExecution);
        Task<Execution> CreateExecutionModelFlow(int id, Execution? parentExecution);
        Task ExecuteFlowStepAction(Execution execution);
        Task<FlowStep?> GetNextChildFlowStep(Execution execution);
        Task<FlowStep?> GetNextSiblingFlowStep(Execution execution);
        Task SetExecutionModelStateRunning(Execution execution);
        Task SetExecutionModelStateComplete(Execution execution);
        Task SaveToDisk(Execution execution);
    }
}
