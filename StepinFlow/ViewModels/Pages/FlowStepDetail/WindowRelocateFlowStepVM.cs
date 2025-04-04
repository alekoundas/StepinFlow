﻿using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Model.Models;
using Model.Structs;
using Business.Helpers;
using Business.BaseViewModels;
using Business.Services.Interfaces;
using Business.Factories.FormValidationFactory;

namespace StepinFlow.ViewModels.Pages
{
    public partial class WindowRelocateFlowStepVM : BaseFlowStepDetailVM
    {
        private readonly ISystemService _systemService;
        private readonly IDataService _dataService;
        private readonly IFormValidationFactory _formValidationFactory;

        [ObservableProperty]
        private List<string> _processList = SystemProcessHelper.GetProcessWindowTitles();

        public WindowRelocateFlowStepVM(
            ISystemService systemService,
            IDataService dataService,
            IFormValidationFactory formValidationFactory) : base(dataService)
        {
            _dataService = dataService;
            _systemService = systemService;
            _formValidationFactory = formValidationFactory;
        }

        public override async Task LoadNewFlowStep(FlowStep newFlowStep)
        {
            ValidationHelper.ErrorsChanged += OnErrorsChange;
            FlowStep = newFlowStep;
            FlowStep.Name = "Window relocate.";
        }


        [RelayCommand]
        private void OnButtonRecordClick()
        {
            if (FlowStep.ProcessName.Length <= 1)
                return;

            Rectangle? windowRect = _systemService.GetWindowSize(FlowStep.ProcessName);

            FlowStep.LocationX = windowRect.Value.Left;
            FlowStep.LocationY = windowRect.Value.Top;
        }

        [RelayCommand]
        private void OnButtonRefreshClick()
        {
            ProcessList = SystemProcessHelper.GetProcessWindowTitles();
        }

        [RelayCommand]
        private void OnButtonTestClick()
        {
            if (FlowStep.ProcessName.Length <= 1)
                return;

            Rectangle? windowRect = _systemService.GetWindowSize(FlowStep.ProcessName);
            Rectangle newWindowRect = new Rectangle();

            int height = Math.Abs(windowRect.Value.Bottom - windowRect.Value.Top);
            int width = Math.Abs(windowRect.Value.Left - windowRect.Value.Right);

            newWindowRect.Left = FlowStep.LocationX;
            newWindowRect.Top = FlowStep.LocationY;
            newWindowRect.Right = FlowStep.LocationX + width;
            newWindowRect.Bottom = FlowStep.LocationY + height;

            _systemService.MoveWindow(FlowStep.ProcessName, newWindowRect);
        }

        public override async Task<int> OnSave()
        {
            ValidationHelper.ClearErrors();
            _formValidationFactory.CreateValidator("FlowStep.Name").Validate(FlowStep.Name);
            _formValidationFactory.CreateValidator("FlowStep.ProcessName").Validate(FlowStep.ProcessName);
            _formValidationFactory.CreateValidator("FlowStep.LocationX").Validate(FlowStep.LocationX);
            _formValidationFactory.CreateValidator("FlowStep.LocationY").Validate(FlowStep.LocationY);

            if (ValidationHelper.HasErrors())
                return -1;


            // Edit mode
            if (FlowStep.Id > 0)
            {
                FlowStep updateFlowStep = await _dataService.FlowSteps.FirstAsync(x => x.Id == FlowStep.Id);
                updateFlowStep.Name = FlowStep.Name;
                updateFlowStep.LocationY = FlowStep.LocationY;
                updateFlowStep.LocationX = FlowStep.LocationX;
                updateFlowStep.ProcessName = FlowStep.ProcessName;
                updateFlowStep.Type = FlowStep.Type;
                await _dataService.UpdateAsync(updateFlowStep);
            }

            /// Add mode
            else
            {
                FlowStep isNewSimpling;

                if (FlowStep.ParentFlowStepId != null)
                    isNewSimpling = await _dataService.FlowSteps.GetIsNewSibling(FlowStep.ParentFlowStepId.Value);
                else if (FlowStep.FlowId.HasValue)
                    isNewSimpling = await _dataService.Flows.GetIsNewSibling(FlowStep.FlowId.Value);
                else
                    return -1;

                FlowStep.OrderingNum = isNewSimpling.OrderingNum;
                isNewSimpling.OrderingNum++;
                await _dataService.UpdateAsync(isNewSimpling);

                await _dataService.FlowSteps.AddAsync(FlowStep);
            }
            return FlowStep.Id;
        }
    }
}
