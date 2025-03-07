using Business.Services.Interfaces;
using Model.Enums;
using Model.Models;

namespace Business.Factories.Workers
{
    public class CursorClickExecutionWorker : CommonExecutionWorker, IExecutionWorker
    {
        private readonly IDataService _dataService;
        private readonly ISystemService _systemService;

        public CursorClickExecutionWorker(IDataService dataService, ISystemService systemService) : base(dataService, systemService)
        {
            _dataService = dataService;
            _systemService = systemService;
        }

        public Task ExecuteFlowStepAction(Execution execution)
        {
            if (execution.FlowStep.CursorButton == null)
            return Task.CompletedTask;

            switch (execution.FlowStep?.CursorAction)
            {
                case MouseActionsEnum.SINGLE_CLICK:
                    _systemService.CursorClick(execution.FlowStep.CursorButton.Value);
                    break;
                case MouseActionsEnum.DOUBLE_CLICK:
                    _systemService.CursorClick(execution.FlowStep.CursorButton.Value);
                    _systemService.CursorClick(execution.FlowStep.CursorButton.Value);
                    break;
                // TODO
                case MouseActionsEnum.LOOP_CLICK:
                    do
                    {
                        _systemService.CursorClick(execution.FlowStep.CursorButton.Value);
                    } while (true);
                default:
                    break;
            }

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
