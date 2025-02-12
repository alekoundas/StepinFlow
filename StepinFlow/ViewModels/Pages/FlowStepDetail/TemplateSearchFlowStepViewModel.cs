﻿using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Model.Models;
using Business.Interfaces;
using Business.Helpers;
using Model.Business;
using Model.Enums;
using DataAccess.Repository.Interface;
using System.Collections.ObjectModel;
using System.IO;
using System.Drawing;
using StepinFlow.Interfaces;

namespace StepinFlow.ViewModels.Pages
{
    public partial class TemplateSearchFlowStepViewModel : ObservableObject, IFlowStepViewModel
    {
        private readonly ISystemService _systemService;
        private readonly ITemplateSearchService _templateMatchingService;
        private readonly IBaseDatawork _baseDatawork;
        private readonly IWindowService _windowService;
        private readonly FlowsViewModel _flowsViewModel;

        [ObservableProperty]
        private List<string> _processList = SystemProcessHelper.GetProcessWindowTitles();

        [ObservableProperty]
        private FlowStep _flowStep = new FlowStep();

        public event ShowResultImageEvent? ShowResultImage;
        public delegate void ShowResultImageEvent(string filePath);

        public TemplateSearchFlowStepViewModel(
            FlowsViewModel flowsViewModel,
            ISystemService systemService,
            ITemplateSearchService templateMatchingService,
            IBaseDatawork baseDatawork,
            IWindowService windowService)
        {

            _baseDatawork = baseDatawork;
            _systemService = systemService;
            _templateMatchingService = templateMatchingService;
            _windowService = windowService;
            _flowsViewModel = flowsViewModel;
        }


        public async Task LoadFlowStepId(int flowStepId)
        {
            FlowStep? flowStep = await _baseDatawork.FlowSteps.FirstOrDefaultAsync(x => x.Id == flowStepId);
            if (flowStep != null)
                FlowStep = flowStep;
        }

        public void LoadNewFlowStep(FlowStep newFlowStep)
        {
            FlowStep = newFlowStep;
        }



        [RelayCommand]
        private void OnButtonOpenFileClick()
        {
            Microsoft.Win32.OpenFileDialog openFileDialog = new Microsoft.Win32.OpenFileDialog();
            openFileDialog.InitialDirectory = PathHelper.GetAppDataPath();
            openFileDialog.Filter = "Image files (*.png)|*.png|All Files (*.*)|*.*";
            openFileDialog.RestoreDirectory = true;

            if (openFileDialog.ShowDialog() == true)
            {
                FlowStep.TemplateImage = File.ReadAllBytes(openFileDialog.FileName);
            }
        }

        [RelayCommand]
        private async Task OnButtonTakeScreenshotClick()
        {
            byte[]? resultTemplate = await _windowService.OpenScreenshotSelectionWindow();
            if (resultTemplate == null)
                return;

            FlowStep.TemplateImage = resultTemplate;
        }


        [RelayCommand]
        private void OnButtonTestClick()
        {
            // Find search area.
            Model.Structs.Rectangle searchRectangle;
            if (FlowStep.ProcessName.Length > 0 )
                searchRectangle = _systemService.GetWindowSize(FlowStep.ProcessName);
            else
                searchRectangle = _systemService.GetScreenSize();

            // Get screenshot.
            Bitmap? screenshot = _systemService.TakeScreenShot(searchRectangle);
            if (screenshot == null)
                return;

            using (var ms = new MemoryStream(FlowStep.TemplateImage))
            {
                Bitmap templateImage = new Bitmap(ms);

                TemplateMatchingResult result = _templateMatchingService.SearchForTemplate(templateImage, screenshot, false);


                int x = searchRectangle.Left;
                int y = searchRectangle.Top;

                x += result.ResultRectangle.Top;
                y += result.ResultRectangle.Left;


                if (result.ResultImagePath.Length > 1)
                    ShowResultImage?.Invoke(result.ResultImagePath);
            }
        }

        [RelayCommand]
        private void OnButtonCancelClick()
        {
            //TODO
        }

        [RelayCommand]
        private async Task OnButtonSaveClick()
        {
            // Edit mode
            if (FlowStep.Id > 0)
            {
                FlowStep updateFlowStep = await _baseDatawork.FlowSteps.FirstAsync(x => x.Id == FlowStep.Id);
                updateFlowStep.Accuracy = FlowStep.Accuracy;
                updateFlowStep.Name = FlowStep.Name;
                updateFlowStep.ProcessName = FlowStep.ProcessName;
                updateFlowStep.TemplateImagePath = FlowStep.TemplateImagePath;
            }

            /// Add mode
            else
            {
                FlowStep isNewSimpling;

                if (FlowStep.ParentFlowStepId != null)
                    isNewSimpling = await _baseDatawork.FlowSteps.GetIsNewSibling(FlowStep.ParentFlowStepId.Value);
                else if (FlowStep.FlowId.HasValue)
                    isNewSimpling = await _baseDatawork.Flows.GetIsNewSibling(FlowStep.FlowId.Value);
                else
                    return;

                FlowStep.OrderingNum = isNewSimpling.OrderingNum;
                isNewSimpling.OrderingNum++;
                await _baseDatawork.SaveChangesAsync();



                // "Add" Flow steps
                FlowStep newFlowStep = new FlowStep();
                FlowStep newFlowStep2 = new FlowStep();
                newFlowStep.FlowStepType = FlowStepTypesEnum.IS_NEW;
                newFlowStep2.FlowStepType = FlowStepTypesEnum.IS_NEW;

                // "Success" Flow step
                FlowStep successFlowStep = new FlowStep();
                successFlowStep.Name = "Success";
                successFlowStep.IsExpanded = false;
                successFlowStep.FlowStepType = FlowStepTypesEnum.IS_SUCCESS;
                successFlowStep.ChildrenFlowSteps = new ObservableCollection<FlowStep>
                {
                    newFlowStep
                };

                // "Fail" Flow step
                FlowStep failFlowStep = new FlowStep();
                failFlowStep.Name = "Fail";
                failFlowStep.IsExpanded = false;
                failFlowStep.FlowStepType = FlowStepTypesEnum.IS_FAILURE;
                failFlowStep.ChildrenFlowSteps = new ObservableCollection<FlowStep>
                {
                    newFlowStep2
                };

                FlowStep.ChildrenFlowSteps = new ObservableCollection<FlowStep>
                {
                    successFlowStep,
                    failFlowStep
                };

                if (FlowStep.Name.Length == 0)
                    FlowStep.Name = "Template search";

                FlowStep.IsExpanded = true;

                _baseDatawork.FlowSteps.Add(FlowStep);

                await _baseDatawork.SaveChangesAsync();
                await _flowsViewModel.RefreshData();
            }
        }

    }
}

