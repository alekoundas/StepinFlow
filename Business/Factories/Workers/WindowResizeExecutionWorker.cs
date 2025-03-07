using Business.Services.Interfaces;
using Model.Enums;
using Model.Models;
using Model.Structs;

namespace Business.Factories.Workers
{
    public class WindowResizeExecutionWorker : CommonExecutionWorker, IExecutionWorker
    {
        private readonly IDataService _dataService;
        private readonly ISystemService _systemService;

        public WindowResizeExecutionWorker(IDataService dataService, ISystemService systemService)
            : base(dataService, systemService)
        {
            _dataService = dataService;
            _systemService = systemService;
        }

        public Task ExecuteFlowStepAction(Execution execution)
        {
            if (execution.FlowStep?.ProcessName.Length <= 1 || execution.FlowStep == null)
                return Task.CompletedTask;

            Rectangle? windowRect = _systemService.GetWindowSize(execution.FlowStep.ProcessName);
            Rectangle newWindowRect = new Rectangle();
            if (windowRect == null)
                return Task.CompletedTask;

            newWindowRect.Left = windowRect.Value.Left;
            newWindowRect.Top = windowRect.Value.Top;
            newWindowRect.Right = windowRect.Value.Left + execution.FlowStep.Width;
            newWindowRect.Bottom = windowRect.Value.Top + execution.FlowStep.Height;

            bool result = _systemService.MoveWindow(execution.FlowStep.ProcessName, newWindowRect);

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
