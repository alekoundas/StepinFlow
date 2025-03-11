using Business.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Model.Enums;
using Model.Models;

namespace Business.Factories.Workers
{
    public class LoopExecutionWorker : CommonExecutionWorker, IExecutionWorker
    {
        private readonly IDataService _dataService;
        private readonly ISystemService _systemService;

        public LoopExecutionWorker(
              IDataService dataService
            , ISystemService systemService
            ) : base(dataService, systemService)
        {
            _dataService = dataService;
            _systemService = systemService;
        }

        public async override Task<Execution> CreateExecutionModel(FlowStep flowStep, Execution parentExecution)
        {
            Execution execution = new Execution
            {
                FlowStepId = flowStep.Id,
                ParentExecutionId = parentExecution.Id,
                ParentLoopExecutionId = parentExecution.Id,
                ExecutionFolderDirectory = parentExecution.ExecutionFolderDirectory,
                LoopCount = parentExecution.LoopCount == null ? 0 : parentExecution.LoopCount + 1
            };


            await _dataService.Executions.AddAsync(execution);

            parentExecution.ChildExecutionId = execution.Id;
            _dataService.Update(parentExecution);

            execution.FlowStep = flowStep;
            execution.ParentExecution = null;
            return execution;
        }

        public Task ExecuteFlowStepAction(Execution execution)
        {
            return Task.CompletedTask;
        }

        public async override Task<FlowStep?> GetNextChildFlowStep(Execution execution)
        {
            if (execution.FlowStepId == null)
                return await Task.FromResult<FlowStep?>(null);

            FlowStep? nextFlowStep = await _dataService.FlowSteps.GetNextChild(execution.FlowStepId.Value, null);
            return nextFlowStep;
        }

        public async Task<FlowStep?> GetNextSiblingFlowStep(Execution execution)
        {
            if (execution.FlowStep == null)
                return await Task.FromResult<FlowStep?>(null);

            // If MaxLoopCount is 0 or CurrentLoopCount < MaxLoopCount, return te same flow step.
            if (execution.FlowStep.LoopMaxCount == 0 || execution.LoopCount <= execution.FlowStep.LoopMaxCount)
                return execution.FlowStep;


            // If not, get next sibling flow step. 
            FlowStep? nextFlowStep = await _dataService.FlowSteps.GetNextSibling(execution.FlowStep.Id);
            return nextFlowStep;
        }
    }
}