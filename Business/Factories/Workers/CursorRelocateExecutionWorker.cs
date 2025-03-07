using Business.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Model.Models;
using Model.Structs;

namespace Business.Factories.Workers
{
    public class CursorRelocateExecutionWorker : CommonExecutionWorker, IExecutionWorker
    {
        private readonly IDataService _dataService;
        private readonly ISystemService _systemService;

        public CursorRelocateExecutionWorker(IDataService dataService, ISystemService systemService) : base(dataService, systemService)
        {
            _dataService = dataService;
            _systemService = systemService;
        }

        public async Task ExecuteFlowStepAction(Execution execution)
        {
            if (execution.FlowStep == null)
                return;

            Point pointToMove = new Point();
            Execution? parentExecution = null;
            Execution? currentExecution = execution;

            // Get point from result of parent template search.
            if (execution.FlowStep.ParentTemplateSearchFlowStepId != null)
                while (currentExecution.ParentExecutionId != null)
                {
                    currentExecution = await _dataService.Executions.Query
                        .Include(x => x.FlowStep)
                        .FirstAsync(x => x.Id == currentExecution.ParentExecutionId.Value);

                    if (currentExecution.FlowStepId == execution.FlowStep.ParentTemplateSearchFlowStepId)
                    {
                        parentExecution = currentExecution;
                        break;
                    }

                    if (currentExecution.FlowStep?.ParentTemplateSearchFlowStepId == execution.FlowStep.ParentTemplateSearchFlowStepId)
                    {
                        parentExecution = currentExecution;
                        break;
                    }
                }

            // If parentExecution exists get value from result.
            // Else get point from flow step.
            switch (execution.FlowStep.CursorRelocationType)
            {
                case Model.Enums.CursorRelocationTypesEnum.USE_PARENT_RESULT:
                    if (parentExecution?.ResultLocationX != null && parentExecution?.ResultLocationY != null)
                        pointToMove = new Point(parentExecution.ResultLocationX.Value, parentExecution.ResultLocationY.Value);
                    break;
                case Model.Enums.CursorRelocationTypesEnum.CUSTOM:
                    pointToMove = new Point(execution.FlowStep.LocationX, execution.FlowStep.LocationY);
                    break;
                case null:
                    return;
            }

            _systemService.SetCursorPossition(pointToMove);
            execution.ResultLocationX = pointToMove.X;
            execution.ResultLocationY = pointToMove.Y;
            await _dataService.UpdateAsync(execution);
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
