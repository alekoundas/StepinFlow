using Business.Factories.ExecutionFactory;
using Business.Services.Interfaces;
using Model.Models;

namespace Business.Factories.ExecutionFactory.Workers
{
    public class WaitExecutionWorker : CommonExecutionWorker, IExecutionWorker
    {
        private readonly IDataService _dataService;
        private readonly ISystemService _systemService;
        private CancellationTokenSource _cancellationToken;


        public WaitExecutionWorker(IDataService dataService, ISystemService systemService, CancellationTokenSource cancellationToken) : base(dataService, systemService)
        {
            _dataService = dataService;
            _systemService = systemService;
            _cancellationToken = cancellationToken;
        }

        public async Task ExecuteFlowStepAction(Execution execution)
        {
            if (execution.FlowStep == null)
                return;

            try
            {
                await Task.Delay(execution.FlowStep.Milliseconds, _cancellationToken.Token);
            }
            catch (TaskCanceledException) { return; }
        }

        public async Task<FlowStep?> GetNextSiblingFlowStep(Execution execution)
        {
            if (execution.FlowStep == null)
                return await Task.FromResult<FlowStep?>(null);

            // Get next sibling flow step. 
            FlowStep? nextFlowStep = await _dataService.FlowSteps.GetNextSibling(execution.FlowStep.Id);
            return nextFlowStep;
        }
    }
}
