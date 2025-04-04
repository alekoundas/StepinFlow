﻿using Business.Extensions;
using Business.Factories.ExecutionFactory;
using Business.Helpers;
using Business.Services.Interfaces;
using Model.Business;
using Model.Enums;
using Model.Models;
using System.Drawing;

namespace Business.Factories.ExecutionFactory.Workers
{
    public class MultipleTemplateSearchExecutionWorker : CommonExecutionWorker, IExecutionWorker
    {
        private readonly IDataService _dataService;
        private readonly ITemplateSearchService _templateSearchService;
        private readonly ISystemService _systemService;
        private readonly ISystemSettingsService _systemSettingsService;

        private byte[]? _resultImage = null;

        public MultipleTemplateSearchExecutionWorker(
              IDataService dataService
            , ISystemService systemService
            , ITemplateSearchService templateSearchService
            , ISystemSettingsService systemSettingsService
            ) : base(dataService, systemService)
        {
            _dataService = dataService;
            _templateSearchService = templateSearchService;
            _systemService = systemService;
            _systemSettingsService = systemSettingsService;
        }

        public async override Task<Execution> CreateExecutionModel(FlowStep flowStep, Execution parentExecution)
        {
            int loopCount = 0;
            if (parentExecution.FlowStepId == flowStep.Id)
                loopCount = parentExecution.LoopCount.Value + 1;

            // Get first child template search flow step that isnt completed.
            FlowStep? childTemplateSearchFlowStep = await GetChildTemplateSearchFlowStep(flowStep.Id, parentExecution.Id);

            Execution execution = new Execution
            {
                FlowStepId = childTemplateSearchFlowStep?.Id,
                ParentExecutionId = parentExecution.Id,
                ExecutionFolderDirectory = parentExecution.ExecutionFolderDirectory,
                LoopCount = loopCount,
            };

            // Save execution.
            await _dataService.Executions.AddAsync(execution);

            // Save relation IDs
            parentExecution.ChildExecutionId = execution.Id;
            await _dataService.UpdateAsync(execution);

            // Return execution with relations.
            execution.FlowStep = childTemplateSearchFlowStep;
            if (execution.ParentExecution != null)
            {
                execution.ParentExecution.ChildExecution = null;
                execution.ParentExecution = null;
            }

            return execution;
        }

        public async Task ExecuteFlowStepAction(Execution execution)
        {
            if (execution.FlowStep?.ParentTemplateSearchFlowStep == null || execution.FlowStep.ParentTemplateSearchFlowStep.TemplateMatchMode == null)
                return;

            // Find search area.
            Model.Structs.Rectangle? searchRectangle = null;
            switch (execution.FlowStep.ParentTemplateSearchFlowStep.FlowParameter?.TemplateSearchAreaType)
            {
                case TemplateSearchAreaTypesEnum.SELECT_EVERY_MONITOR:
                    searchRectangle = _systemService.GetScreenSize();
                    break;
                case TemplateSearchAreaTypesEnum.SELECT_MONITOR:
                    searchRectangle = _systemService.GetMonitorArea(execution.FlowStep.ParentTemplateSearchFlowStep.FlowParameter.SystemMonitorDeviceName);
                    break;
                case TemplateSearchAreaTypesEnum.SELECT_APPLICATION_WINDOW:
                    searchRectangle = _systemService.GetWindowSize(execution.FlowStep.ParentTemplateSearchFlowStep.FlowParameter.ProcessName);
                    break;
                case TemplateSearchAreaTypesEnum.SELECT_CUSTOM_AREA:
                    break;
                default:
                    searchRectangle = _systemService.GetScreenSize();
                    break;
            }

            if (searchRectangle == null)
                searchRectangle = _systemService.GetScreenSize();



            // Get screenshot.
            // New if previous doenst exists.
            // Get previous one if exists and is loop with RemoveTemplateFromResult = true.
            byte[]? screenshot = null;
            Execution? parentLoopExecution = null;
            if (_pendingExecutionLoops.ContainsKey(execution.FlowStep.ParentTemplateSearchFlowStepId.Value))
                parentLoopExecution = _pendingExecutionLoops[execution.FlowStep.ParentTemplateSearchFlowStepId.Value].OrderByDescending(x => x.Id).FirstOrDefault();

            bool canUseParentResult =
               parentLoopExecution?.FlowStep?.IsLoop == true &&
               parentLoopExecution.TempResultImagePath?.Length > 0 &&
               execution.FlowStep.RemoveTemplateFromResult;

            if (canUseParentResult)
                screenshot = Image.FromFile(parentLoopExecution.TempResultImagePath).ToByteArray();
            else
                screenshot = _systemService.TakeScreenShot(searchRectangle.Value);

            if (screenshot == null)
                return;



            TemplateMatchingResult result = _templateSearchService.SearchForTemplate(execution.FlowStep.TemplateImage, screenshot, execution.FlowStep.ParentTemplateSearchFlowStep.TemplateMatchMode, execution.FlowStep.RemoveTemplateFromResult);
            ImageSizeResult imageSizeResult = _systemService.GetImageSize(execution.FlowStep.TemplateImage);

            int x = searchRectangle.Value.Left + result.ResultRectangle.Left + imageSizeResult.Width / 2;
            int y = searchRectangle.Value.Top + result.ResultRectangle.Top + imageSizeResult.Height / 2;
            bool isSuccessful = execution.FlowStep.Accuracy <= result.Confidence;

            execution.Result = isSuccessful ? ExecutionResultEnum.SUCCESS : ExecutionResultEnum.FAIL;
            execution.ResultLocationX = x;
            execution.ResultLocationY = y;
            //execution.ResultImagePath = result.ResultImagePath;
            execution.ResultAccuracy = result.Confidence;

            await _dataService.UpdateAsync(execution);
            _resultImage = result.ResultImage;



            // Add execution to pending loop executions.
            if (!_pendingExecutionLoops.ContainsKey(execution.FlowStep.ParentTemplateSearchFlowStepId.Value))
                _pendingExecutionLoops.Add(execution.FlowStep.ParentTemplateSearchFlowStepId.Value, new List<Execution>() { execution });
            else
                _pendingExecutionLoops[execution.FlowStep.ParentTemplateSearchFlowStepId.Value].Add(execution);
        }

        public async override Task<FlowStep?> GetNextChildFlowStep(Execution execution)
        {
            if (execution.FlowStep?.ParentTemplateSearchFlowStepId == null)
                return await Task.FromResult<FlowStep?>(null);

            FlowStep? nextFlowStep = await _dataService.FlowSteps.GetNextChild(execution.FlowStep.ParentTemplateSearchFlowStepId.Value, execution.Result);
            return nextFlowStep;
        }

        public async Task<FlowStep?> GetNextSiblingFlowStep(Execution execution)
        {
            if (execution.FlowStep?.ParentTemplateSearchFlowStepId == null)
                return await Task.FromResult<FlowStep?>(null);

            // Get next child TemplateSearchFlowStep.
            FlowStep? nextChildTemplateSearchFlowStep = await GetChildTemplateSearchFlowStep(execution.FlowStep.ParentTemplateSearchFlowStepId.Value, execution.Id);

            if (nextChildTemplateSearchFlowStep != null)
                return execution.FlowStep.ParentTemplateSearchFlowStep;


            // Delete temp images since execution is completed.
            foreach (Execution parentLoopExecution in _pendingExecutionLoops[execution.FlowStep.ParentTemplateSearchFlowStepId.Value])
            {
                try
                {
                    string filePath = parentLoopExecution.TempResultImagePath;
                    if (!string.IsNullOrEmpty(filePath) && File.Exists(filePath))
                    {
                        int retries = 3;
                        while (retries > 0)
                        {
                            try
                            {
                                File.Delete(filePath);
                                break; 
                            }
                            catch (UnauthorizedAccessException)
                            {
                                break;
                            }
                            catch (IOException)
                            {
                                // File might be in use - wait and retry
                                retries--;
                                if (retries > 0)
                                    Thread.Sleep(100); // Wait 100ms before retry
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    continue;
                }
            }

            _pendingExecutionLoops[execution.FlowStep.ParentTemplateSearchFlowStepId.Value].Clear();

            // If not, get next sibling flow step. 
            FlowStep? nextFlowStep = await _dataService.FlowSteps.GetNextSibling(execution.FlowStep.ParentTemplateSearchFlowStepId.Value);
            return nextFlowStep;
        }

        public async override Task SaveToDisk(Execution execution)
        {
            if (!execution.ParentExecutionId.HasValue || execution.ExecutionFolderDirectory.Length == 0 || !execution.StartedOn.HasValue)
                return;

            bool allowExecutionImageSave = bool.Parse(_systemSettingsService.GetSetting(AppSettingsEnum.IS_EXECUTION_HISTORY_LOG_ENABLED).Value);
            double saveImageQuality= double.Parse(_systemSettingsService.GetSetting(AppSettingsEnum.EXECUTION_HISTORY_LOG_IMAGE_QUALITY).Value);
            string fileDate = execution.StartedOn.Value.ToString("dd-MM-yyyy hh.mm.ss.fff");
            string newFilePath = Path.Combine(execution.ExecutionFolderDirectory, fileDate + ".png");

            // Save image to disk (History).
            if (_resultImage != null && allowExecutionImageSave)
                execution.ResultImagePath = await _systemService.SaveImageToDisk(newFilePath, _resultImage, saveImageQuality);

            // Save image to disk (Temp).
            if (execution.Result == ExecutionResultEnum.SUCCESS)
            {
                string tempFilePath = Path.Combine(PathHelper.GetTempDataPath(), fileDate + ".png");
                if (_resultImage != null)
                    await _systemService.SaveImageToDisk(tempFilePath, _resultImage);

                execution.TempResultImagePath = tempFilePath;
            }


            await _dataService.UpdateAsync(execution);
        }

        private async Task<FlowStep?> GetChildTemplateSearchFlowStep(int flowStepId, int parentExecutionId)
        {
            // Get all parents of loop execution.
            List<Execution> parentLoopExecutions = new List<Execution>();
            if (_pendingExecutionLoops.ContainsKey(flowStepId))
                parentLoopExecutions = _pendingExecutionLoops[flowStepId];


            // Get all completed children template flow steps.
            List<int> completedChildrenTemplateFlowStepIds = parentLoopExecutions
                .Where(x => !x.FlowStep.IsLoop || x.FlowStep.IsLoop && x.Result == ExecutionResultEnum.FAIL)
                .Select(x => x.FlowStepId ?? 0)
                .Where(x => x != 0)
                .ToList();

            // Get all child template search flow steps.
            List<FlowStep> children = await _dataService.FlowSteps
                .Where(x => x.ParentTemplateSearchFlowStepId == flowStepId)
                .Where(x => x.Type == FlowStepTypesEnum.MULTIPLE_TEMPLATE_SEARCH_CHILD)
                .Include(x => x.ParentTemplateSearchFlowStep!.FlowParameter)
                .ToListAsync();

            // Get first child template search flow step that isnt completed.
            FlowStep? flowStep = children
                .Where(x => !completedChildrenTemplateFlowStepIds.Any(y => y == x.Id))
                .ToList()
                .OrderBy(x => x.OrderingNum).ThenBy(x => x.Id)
                .FirstOrDefault();


            return flowStep;
        }
    }
}