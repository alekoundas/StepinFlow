﻿using Business.Factories.ExecutionFactory;
using Business.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Model.Enums;
using Model.Models;

namespace Business.Factories.ExecutionFactory.Workers
{
    public class SubFlowStepExecutionWorker : CommonExecutionWorker, IExecutionWorker
    {
        private readonly IDataService _dataService;
        private readonly ISystemService _systemService;

        public SubFlowStepExecutionWorker(IDataService dataService, ISystemService systemService) : base(dataService, systemService)
        {
            _dataService = dataService;
            _systemService = systemService;
        }

        public Task ExecuteFlowStepAction(Execution execution)
        {
            // Nothing to execute.
            return Task.CompletedTask; 
        }

        public async override Task<FlowStep?> GetNextChildFlowStep(Execution execution)
        {
            FlowStep? nextFlowStep = await _dataService.FlowSteps
                            .Where(x => x.FlowId == execution.FlowStep.SubFlowId)
                            .SelectMany<FlowStep>(x => x.ChildrenFlowSteps)
                            .Where(x => x.Type != FlowStepTypesEnum.NEW)
                            .OrderBy(x => x.OrderingNum)
                            .FirstOrDefaultAsync();

            //TODO return error message 
            if (nextFlowStep == null)
                return null;

            return nextFlowStep;
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
