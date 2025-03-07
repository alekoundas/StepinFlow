using Business.DatabaseContext;
using Business.Extensions;
using Business.Helpers;
using Business.Services.Interfaces;
using Model.Enums;
using Model.Models;
using System.Collections.ObjectModel;

namespace Business.Factories.Workers
{
    public class CommonExecutionWorker
    {
        private readonly IExecutionDataService _dataService;
        private readonly ISystemService _systemService;
        protected Dictionary<int, List<Execution>> _pendingExecutionLoops = new Dictionary<int, List<Execution>>();

        public CommonExecutionWorker(IExecutionDataService dataService, ISystemService systemService)
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


            string folderName = "Execution - " + DateTime.Now.ToString("yy-MM-dd hh.mm");
            string folderPath = Path.Combine(PathHelper.GetExecutionHistoryDataPath(), folderName);

            execution.ExecutionFolderDirectory = folderPath;
            await _dataService.Executions.AddAsync(execution);

            return execution;
        }

        public async virtual Task<Execution> CreateExecutionModel(FlowStep flowStep, Execution parentExecution)
        {
            Execution execution = new Execution();
            execution.FlowStepId = flowStep.Id;
            execution.ParentExecutionId = parentExecution.Id;
            execution.ParentLoopExecutionId = parentExecution.Id;
            execution.ExecutionFolderDirectory = parentExecution.ExecutionFolderDirectory;
            await _dataService.Executions.AddAsync(execution);


            parentExecution.ChildExecutionId = execution.Id;
            await _dataService.UpdateAsync(parentExecution);


            execution.FlowStep = flowStep;
            return execution;
        }

        public async virtual Task SetExecutionModelStateRunning(Execution execution)
        {
            execution.Status = ExecutionStatusEnum.RUNNING;
            execution.StartedOn = DateTime.Now;
            execution.LoopCount += 1;

            await _dataService.UpdateAsync(execution);
        }

        public async virtual Task SetExecutionModelStateComplete(Execution execution)
        {
            execution.Status = ExecutionStatusEnum.COMPLETED;
            execution.EndedOn = DateTime.Now;

            await _dataService.UpdateAsync(execution);
        }

        public virtual Task ExpandAndSelectFlowStep(Execution execution, ObservableCollection<Flow> treeviewFlows)
        {
            if (execution.FlowStep == null)
                return Task.CompletedTask;

            //Application.Current.Dispatcher.Invoke(() =>
            //{
            // Code to update ObservableCollection
            FlowStep? uiFlowStep = treeviewFlows.First()
                .Descendants()
                .FirstOrDefault(x => x.Id == execution.FlowStepId);

            if (uiFlowStep != null)
            {
                uiFlowStep.IsExpanded = true;
                uiFlowStep.IsSelected = true;
            }

            if (uiFlowStep?.ParentFlowStep != null)
                uiFlowStep.ParentFlowStep.IsExpanded = true;
            if (uiFlowStep?.Flow != null)
                uiFlowStep.Flow.IsExpanded = true;
            //});

            return Task.CompletedTask;
        }

        public async virtual Task<FlowStep?> GetNextChildFlowStep(Execution execution)
        {
            return await Task.FromResult<FlowStep?>(null);
        }


        public async virtual Task SaveToDisk(Execution execution)
        {
            await _dataService.SaveChangesAsync();
        }

        public void ClearEntityFrameworkChangeTracker()
        {
            //_dataService.Query.ChangeTracker.Clear();
        }
    }
}
