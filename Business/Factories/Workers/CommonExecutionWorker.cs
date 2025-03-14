using Business.DatabaseContext;
using Business.Helpers;
using Business.Services.Interfaces;
using Model.Enums;
using Model.Models;

namespace Business.Factories.Workers
{
    public class CommonExecutionWorker
    {
        private readonly IDataService _dataService;
        private readonly ISystemService _systemService;
        protected Dictionary<int, List<Execution>> _pendingExecutionLoops = new Dictionary<int, List<Execution>>();

        public CommonExecutionWorker(IDataService dataService, ISystemService systemService)
        {
            _dataService = dataService;
            _systemService = systemService;
        }
        public void Initialize(Dictionary<int, List<Execution>> pendingExecutionLoops)
        {
            _pendingExecutionLoops = pendingExecutionLoops;
        }

        public void SetDbContext(InMemoryDbContext dbContext)
        {
            _dataService.SetDbContext(dbContext);
        }


        public async Task<Execution> CreateExecutionModelFlow(int flowId, Execution? _)
        {
            Execution execution = new Execution();
            execution.FlowId = flowId;


            string folderName = "Execution - " + DateTime.Now.ToString("dd-MM-yyyy hh.mm");
            string folderPath = Path.Combine(PathHelper.GetExecutionHistoryDataPath(), folderName);

            execution.ExecutionFolderDirectory = folderPath;
            await _dataService.Executions.AddAsync(execution);

            return execution;
        }

        public async virtual Task<Execution> CreateExecutionModel(FlowStep flowStep, Execution parentExecution)
        {
            Execution execution = new Execution
            {
                FlowStepId = flowStep.Id,
                ParentExecutionId = parentExecution.Id,
                ExecutionFolderDirectory = parentExecution.ExecutionFolderDirectory
            };
            await _dataService.Executions.AddAsync(execution);


            parentExecution.ChildExecutionId = execution.Id;
            await _dataService.UpdateAsync(parentExecution);


            execution.FlowStep = flowStep;
            if (execution.ParentExecution != null)
            {
                execution.ParentExecution.ChildExecution = null;
                execution.ParentExecution = null;
            }
            return execution;
        }

        public async virtual Task SetExecutionModelStateRunning(Execution execution)
        {
            execution.Status = ExecutionStatusEnum.RUNNING;
            execution.StartedOn = DateTime.Now;

            await _dataService.UpdateAsync(execution);
        }

        public async virtual Task SetExecutionModelStateComplete(Execution execution)
        {
            execution.Status = ExecutionStatusEnum.COMPLETED;
            execution.EndedOn = DateTime.Now;

            await _dataService.UpdateAsync(execution);
        }

        public virtual Task<FlowStep?> GetNextChildFlowStep(Execution execution)
        {
            return Task.FromResult<FlowStep?>(null);
        }

        public virtual Task SaveToDisk(Execution execution)
        {
            return Task.CompletedTask;
        }
    }
}
