using Business.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Model.Enums;
using Model.Models;

namespace Business.Factories.Workers
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
            FlowStep? nextFlowStep = await _dataService.Flows.Query
                .Where(x=>x.Id == execution.FlowStep.SubFlowId)
                .SelectMany(x=>x.FlowStep.ChildrenFlowSteps)
                .Where(x => x.Type != FlowStepTypesEnum.NEW)
                .OrderBy(x=>x.OrderingNum)
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


        //public async override Task SaveToDisk(Execution execution)
        //{
        //    string folderName = "Execution - " + DateTime.Now.ToString("yy-MM-dd hh.mm");
        //    string folderPath = Path.Combine(PathHelper.GetExecutionHistoryDataPath(), folderName);

        //    execution.ExecutionFolderDirectory = folderPath;

        //    await _dataService.SaveChangesAsync();
        //    Directory.CreateDirectory(folderPath);
        //}
    }
}
