using Business.Helpers;
using Business.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Model.Business;
using Model.Enums;
using Model.Models;
using System.Drawing;

namespace Business.Factories.Workers
{
    public class WaitForTemplateExecutionWorker : CommonExecutionWorker, IExecutionWorker
    {
        private readonly IExecutionDataService _dataService;
        private readonly ITemplateSearchService _templateSearchService;
        private readonly ISystemService _systemService;

        private byte[]? _resultImage = null;

        public WaitForTemplateExecutionWorker(
              IExecutionDataService dataService
            , ISystemService systemService
            , ITemplateSearchService templateSearchService
            ) : base(dataService, systemService)
        {
            _dataService = dataService;
            _templateSearchService = templateSearchService;
            _systemService = systemService;
        }

        public async override Task<Execution> CreateExecutionModel(FlowStep flowStep, Execution parentExecution)
        {
            if (parentExecution == null)
                throw new ArgumentNullException(nameof(parentExecution));

            Execution execution = new Execution
            {
                FlowStepId = flowStep.Id,
                ParentExecutionId = parentExecution.Id,
                ParentLoopExecutionId = parentExecution.Id,
                ExecutionFolderDirectory = parentExecution.ExecutionFolderDirectory,
                LoopCount = parentExecution?.LoopCount == null ? 0 : parentExecution.LoopCount + 1
            };


            await _dataService.Executions.AddAsync(execution);

            parentExecution.ChildExecutionId = execution.Id;
            await _dataService.UpdateAsync(parentExecution);

            execution.FlowStep = flowStep;
            return execution;
        }

        public async Task ExecuteFlowStepAction(Execution execution)
        {
            if (execution.FlowStep == null || execution.FlowStep.TemplateImage == null)
                return;

            // Find search area.
            Model.Structs.Rectangle? searchRectangle = null;
            switch (execution.FlowStep.FlowParameter?.TemplateSearchAreaType)
            {
                case TemplateSearchAreaTypesEnum.SELECT_EVERY_MONITOR:
                    searchRectangle = _systemService.GetScreenSize();
                    break;
                case TemplateSearchAreaTypesEnum.SELECT_MONITOR:
                    searchRectangle = _systemService.GetMonitorArea(execution.FlowStep.FlowParameter.SystemMonitorDeviceName);
                    break;
                case TemplateSearchAreaTypesEnum.SELECT_APPLICATION_WINDOW:
                    searchRectangle = _systemService.GetWindowSize(execution.FlowStep.FlowParameter.ProcessName);
                    break;
                case TemplateSearchAreaTypesEnum.SELECT_CUSTOM_AREA:
                    break;
                default:
                    searchRectangle = _systemService.GetScreenSize();
                    break;
            }

            if (searchRectangle == null)
                searchRectangle = _systemService.GetScreenSize();

            bool isSuccessful = false;
            while (!isSuccessful)
            {
                // Get screenshot.
                byte[]? screenshot = _systemService.TakeScreenShot(searchRectangle.Value);
                if (screenshot == null)
                    return;

                TemplateMatchingResult result = _templateSearchService.SearchForTemplate(execution.FlowStep.TemplateImage, screenshot, execution.FlowStep.TemplateMatchMode, execution.FlowStep.RemoveTemplateFromResult);
                ImageSizeResult imageSizeResult = _systemService.GetImageSize(execution.FlowStep.TemplateImage);

                int x = searchRectangle.Value.Left + result.ResultRectangle.Left + (imageSizeResult.Width / 2);
                int y = searchRectangle.Value.Top + result.ResultRectangle.Top + (imageSizeResult.Height / 2);

                isSuccessful = execution.FlowStep.Accuracy <= result.Confidence;
                execution.Result = isSuccessful ? ExecutionResultEnum.SUCCESS : ExecutionResultEnum.FAIL;
                execution.ResultLocationX = x;
                execution.ResultLocationY = y;
                //execution.ResultImagePath = result.ResultImagePath;
                execution.ResultAccuracy = result.Confidence;

            await _dataService.UpdateAsync(execution);
                _resultImage = result.ResultImage;


                int miliseconds = 0;

                miliseconds += execution.FlowStep.WaitForMilliseconds;
                miliseconds += execution.FlowStep.WaitForSeconds * 1000;
                miliseconds += execution.FlowStep.WaitForMinutes * 60 * 1000;
                miliseconds += execution.FlowStep.WaitForHours * 60 * 60 * 1000;

                _resultImage = result.ResultImage;
                Thread.Sleep(miliseconds);

            }
        }

        public async override Task<FlowStep?> GetNextChildFlowStep(Execution execution)
        {
            if (execution.FlowStepId == null)
                return await Task.FromResult<FlowStep?>(null);

            FlowStep? nextFlowStep = await _dataService.FlowSteps.GetNextChild(execution.FlowStepId.Value, execution.Result);
            return nextFlowStep;
        }

        public async Task<FlowStep?> GetNextSiblingFlowStep(Execution execution)
        {
            if (execution.FlowStep == null)
                return await Task.FromResult<FlowStep?>(null);

            FlowStep? nextFlowStep = await _dataService.FlowSteps.GetNextSibling(execution.FlowStep.Id);
            return nextFlowStep;
        }


        public async override Task SaveToDisk(Execution execution)
        {
            if (!execution.ParentExecutionId.HasValue || execution.ExecutionFolderDirectory.Length == 0)
                return;

            string fileDate = execution.StartedOn.Value.ToString("yy-MM-dd hh.mm.ss.fff");
            string newFilePath = Path.Combine(execution.ExecutionFolderDirectory, fileDate + ".png");

            if (_resultImage != null)
                await _systemService.SaveImageToDisk(newFilePath, _resultImage);

            if (execution.Result == ExecutionResultEnum.SUCCESS)
            {
                string tempFilePath = Path.Combine(PathHelper.GetTempDataPath(), fileDate + ".png");
                if (_resultImage != null)
                    await _systemService.SaveImageToDisk(tempFilePath, _resultImage);

                execution.TempResultImagePath = tempFilePath;
            }

            execution.ResultImagePath = newFilePath;

            await _dataService.UpdateAsync(execution);
        }
    }
}