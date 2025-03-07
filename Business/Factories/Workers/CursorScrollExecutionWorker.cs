using Business.Services.Interfaces;
using Model.Models;

namespace Business.Factories.Workers
{
    public class CursorScrollExecutionWorker : CommonExecutionWorker, IExecutionWorker
    {
        private readonly IDataService _dataService;
        private readonly ISystemService _systemService;

        public CursorScrollExecutionWorker(IDataService dataService, ISystemService systemService) : base(dataService, systemService)
        {
            _dataService = dataService;
            _systemService = systemService;
        }

        public Task ExecuteFlowStepAction(Execution execution)
        {
            if (execution.FlowStep.CursorScrollDirection == null)
                return Task.CompletedTask;


            _systemService.CursorScroll(execution.FlowStep.CursorScrollDirection.Value, execution.FlowStep.LoopCount);

            return Task.CompletedTask;
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
