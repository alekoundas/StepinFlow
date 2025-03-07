using Business.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Model.Models;

namespace Business.Factories.Workers
{
    public class GoToExecutionWorker : CommonExecutionWorker, IExecutionWorker
    {
        private readonly IDataService _dataService;

        public GoToExecutionWorker(IDataService dataService, ISystemService systemService) : base(dataService, systemService)
        {
            _dataService = dataService;
        }

        public Task ExecuteFlowStepAction(Execution execution)
        {
            return Task.CompletedTask;
        }

        public async Task<FlowStep?> GetNextSiblingFlowStep(Execution execution)
        {
            if (execution.FlowStep == null)
                return await Task.FromResult<FlowStep?>(null);
            if (!execution.FlowStep.ParentTemplateSearchFlowStepId.HasValue)
                return await Task.FromResult<FlowStep?>(null);

            FlowStep? nextFlowStep = await _dataService.FlowSteps
                .FirstOrDefaultAsync(x => x.Id == execution.FlowStep.ParentTemplateSearchFlowStepId.Value);

            //TODO return error message 
            if (nextFlowStep == null)
                return null;

            return nextFlowStep;
        }
    }
}
