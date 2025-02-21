﻿using Business.Interfaces;
using DataAccess.Repository.Interface;
using Microsoft.EntityFrameworkCore;
using Model.Business;
using Model.Enums;
using Model.Models;
using System.Drawing;

namespace Business.Factories.Workers
{
    public class TemplateSearchLoopExecutionWorker : CommonExecutionWorker, IExecutionWorker
    {
        private readonly IBaseDatawork _baseDatawork;
        private readonly ITemplateSearchService _templateSearchService;
        private readonly ISystemService _systemService;

        private byte[]? _resultImage = null;

        public TemplateSearchLoopExecutionWorker(
              IBaseDatawork baseDatawork
            , ISystemService systemService
            , ITemplateSearchService templateSearchService
            ) : base(baseDatawork, systemService)
        {
            _baseDatawork = baseDatawork;
            _templateSearchService = templateSearchService;
            _systemService = systemService;
        }

        public async override Task<Execution> CreateExecutionModel(FlowStep flowStep, Execution parentExecution, Execution latestParentExecution)
        {
            if (parentExecution == null)
                throw new ArgumentNullException(nameof(parentExecution));

            Execution execution = new Execution
            {
                FlowStepId = flowStep.Id,
                ParentExecutionId = latestParentExecution.Id,
                ParentLoopExecutionId = parentExecution.Id,
                ExecutionFolderDirectory = parentExecution.ExecutionFolderDirectory,
                LoopCount = parentExecution?.LoopCount == null ? 0 : parentExecution.LoopCount + 1
            };

            _baseDatawork.Executions.Add(execution);
            await _baseDatawork.SaveChangesAsync();

            parentExecution.ChildExecutionId = execution.Id;
            await _baseDatawork.SaveChangesAsync();

            execution.FlowStep = flowStep;
            return execution;
        }

        public async Task ExecuteFlowStepAction(Execution execution)
        {
            if (execution.FlowStep == null || execution.FlowStep.TemplateImage == null)
                return;

            // Find search area.
            Model.Structs.Rectangle searchRectangle;
            if (execution.FlowStep.ProcessName.Length > 0)
                searchRectangle = _systemService.GetWindowSize(execution.FlowStep.ProcessName);
            else
                searchRectangle = _systemService.GetScreenSize();

            // Get screenshot.
            // New if not previous exists.
            // Get previous one if exists.
            Bitmap? screenshot = null;
            Execution? parentLoopExecution = await _baseDatawork.Executions.FirstOrDefaultAsync(x => x.Id == execution.ParentLoopExecutionId);
            if (parentLoopExecution?.ResultImagePath?.Length > 0 && execution.FlowStep.RemoveTemplateFromResult)
                screenshot = (Bitmap)Image.FromFile(parentLoopExecution.ResultImagePath);
            else
                screenshot = _systemService.TakeScreenShot(searchRectangle);

            if (screenshot == null)
                return;

            ImageSizeResult imageSizeResult = _systemService.GetImageSize(execution.FlowStep.TemplateImage);
            using (var ms = new MemoryStream(execution.FlowStep.TemplateImage))
            {
                Bitmap templateImage = new Bitmap(ms);
                TemplateMatchingResult result = _templateSearchService.SearchForTemplate(templateImage, screenshot, execution.FlowStep.RemoveTemplateFromResult);

                int x = searchRectangle.Left + result.ResultRectangle.Left + (imageSizeResult.Width / 2);
                int y = searchRectangle.Top + result.ResultRectangle.Top + (imageSizeResult.Height / 2);

                bool isSuccessful = execution.FlowStep.Accuracy <= result.Confidence;
                execution.ExecutionResultEnum = isSuccessful ? ExecutionResultEnum.SUCCESS : ExecutionResultEnum.FAIL;
                execution.ResultLocationX = x;
                execution.ResultLocationY = y;
                execution.ResultImagePath = result.ResultImagePath;
                execution.ResultAccuracy = result.Confidence;

                await _baseDatawork.SaveChangesAsync();
                _resultImage = result.ResultImage;
            }
        }

        public async override Task<FlowStep?> GetNextChildFlowStep(Execution execution)
        {
            if (execution.FlowStepId == null)
                return await Task.FromResult<FlowStep?>(null);

            FlowStep? nextFlowStep;

            // Get next executable child.
            nextFlowStep = await _baseDatawork.Query.FlowSteps.AsNoTracking()
                .Include(x => x.ChildrenFlowSteps)
                .ThenInclude(x => x.ChildrenFlowSteps)
                .FirstOrDefaultAsync(x => x.Id == execution.FlowStepId);


            if (execution.ExecutionResultEnum == ExecutionResultEnum.SUCCESS)
                nextFlowStep = nextFlowStep?.ChildrenFlowSteps
                    .FirstOrDefault(x => x.Type == FlowStepTypesEnum.SUCCESS)
                    ?.ChildrenFlowSteps
                    .OrderBy(x => x.OrderingNum)
                    .FirstOrDefault(x => x.Type != FlowStepTypesEnum.NEW);
            else
                nextFlowStep = nextFlowStep?.ChildrenFlowSteps
                    .First(x => x.Type == FlowStepTypesEnum.FAILURE)
                    .ChildrenFlowSteps
                    .OrderBy(x => x.OrderingNum)
                    .FirstOrDefault(x => x.Type != FlowStepTypesEnum.NEW);

            nextFlowStep = await _baseDatawork.FlowSteps.GetNextChild(execution.FlowStepId.Value, execution.ExecutionResultEnum);
            return nextFlowStep;
        }

        public async Task<FlowStep?> GetNextSiblingFlowStep(Execution execution)
        {
            if (execution.FlowStep == null)
                return await Task.FromResult<FlowStep?>(null);

            // If execution was successfull and (MaxLoopCount is 0 or CurrentLoopCount < MaxLoopCount), return te same flow step.
            if (execution.ExecutionResultEnum == ExecutionResultEnum.SUCCESS)
            {
                if (execution.FlowStep.MaxLoopCount == 0)
                    return execution.FlowStep;
                else if (execution.LoopCount < execution.FlowStep.MaxLoopCount)
                    return execution.FlowStep;
            }

            // If not, get next sibling flow step. 
            FlowStep? nextFlowStep = await _baseDatawork.FlowSteps.GetNextSibling(execution.FlowStep.Id);
            return nextFlowStep;
        }

        public async override Task SaveToDisk(Execution execution)
        {
            if (!execution.ParentExecutionId.HasValue || execution.ExecutionFolderDirectory.Length == 0)
                return;

            if (execution.StartedOn.HasValue)
            {
                string fileDate = execution.StartedOn.Value.ToString("yy-MM-dd hh.mm.ss.fff");
                string newFilePath = execution.ExecutionFolderDirectory + "\\" + fileDate + ".png";

                //_systemService.CopyImageToDisk(execution.ResultImagePath, newFilePath);_resultImage
                if (_resultImage != null)
                    await _systemService.SaveImageToDisk(newFilePath, _resultImage);
                execution.ResultImagePath = newFilePath;
                await _baseDatawork.SaveChangesAsync();
            }
        }
    }
}