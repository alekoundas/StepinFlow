﻿using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Model.Models;
using Business.Interfaces;
using Model.Structs;
using Business.Helpers;
using DataAccess.Repository.Interface;

namespace StepinFlow.ViewModels.Pages
{
    public partial class WindowMoveFlowStepViewModel : ObservableObject, IFlowStepViewModel
    {
        private readonly ISystemService _systemService;
        private readonly IBaseDatawork _baseDatawork;
        private readonly FlowsViewModel _flowsViewModel;

        [ObservableProperty]
        private FlowStep _flowStep = new FlowStep();

        [ObservableProperty]
        private List<string> _processList = SystemProcessHelper.GetProcessWindowTitles();

        public WindowMoveFlowStepViewModel(FlowsViewModel flowsViewModel, ISystemService systemService, IBaseDatawork baseDatawork)
        {
            _baseDatawork = baseDatawork;
            _systemService = systemService;
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
        private void OnButtonRecordClick()
        {
            if (FlowStep.ProcessName.Length <= 1)
                return;

            Rectangle windowRect = _systemService.GetWindowSize(FlowStep.ProcessName);

            FlowStep.LocationX = windowRect.Left;
            FlowStep.LocationY = windowRect.Top;
        }

        [RelayCommand]
        private void OnButtonTestClick()
        {
            if (FlowStep.ProcessName.Length <= 1)
                return;

            Rectangle windowRect = _systemService.GetWindowSize(FlowStep.ProcessName);
            Rectangle newWindowRect = new Rectangle();

            int height = Math.Abs(windowRect.Bottom - windowRect.Top);
            int width = Math.Abs(windowRect.Left - windowRect.Right);

            newWindowRect.Left = FlowStep.LocationX;
            newWindowRect.Top = FlowStep.LocationY;
            newWindowRect.Right = FlowStep.LocationX + width;
            newWindowRect.Bottom = FlowStep.LocationY + height;

            _systemService.MoveWindow(FlowStep.ProcessName, newWindowRect);
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
                updateFlowStep.Name = FlowStep.Name;
                updateFlowStep.LocationY= FlowStep.LocationY;
                updateFlowStep.LocationX = FlowStep.LocationX;
                updateFlowStep.ProcessName = FlowStep.ProcessName;
                updateFlowStep.FlowStepType = FlowStep.FlowStepType;
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


                if (FlowStep.Name.Length == 0)
                    FlowStep.Name = "Set window location.";

                _baseDatawork.FlowSteps.Add(FlowStep);
            }


            _baseDatawork.SaveChanges();
            await _flowsViewModel.RefreshData();
        }
    }
}
