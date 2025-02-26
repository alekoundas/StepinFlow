﻿using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Model.Models;
using Business.Interfaces;
using Model.Structs;
using Business.Helpers;
using DataAccess.Repository.Interface;
using Business.BaseViewModels;

namespace StepinFlow.ViewModels.Pages
{
    public partial class WindowResizeFlowStepVM : BaseFlowStepDetailVM
    {
        private readonly ISystemService _systemService;
        private readonly IDataService _dataService;
        public override event Action<int> OnSave;

        [ObservableProperty]
        private List<string> _processList = SystemProcessHelper.GetProcessWindowTitles();

        public WindowResizeFlowStepVM(ISystemService systemService, IDataService dataService) : base(dataService)
        {
            _dataService = dataService;
            _systemService = systemService;
        }


        [RelayCommand]
        private void OnButtonRecordClick()
        {
            if (FlowStep.ProcessName.Length <= 1)
                return;

            Rectangle? windowRect = _systemService.GetWindowSize(FlowStep.ProcessName);

            FlowStep.Height = Math.Abs(windowRect.Value.Bottom - windowRect.Value.Top);
            FlowStep.Width = Math.Abs(windowRect.Value.Left - windowRect.Value.Right);
        }

        [RelayCommand]
        private void OnButtonTestClick()
        {
            if (FlowStep.ProcessName.Length <= 1)
                return;

            Rectangle? windowRect = _systemService.GetWindowSize(FlowStep.ProcessName);
            Rectangle newWindowRect = new Rectangle();

            newWindowRect.Left = windowRect.Value.Left;
            newWindowRect.Top = windowRect.Value.Top;
            newWindowRect.Right = windowRect.Value.Left + FlowStep.Width;
            newWindowRect.Bottom = windowRect.Value.Top + FlowStep.Height;

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
            _dataService.Query.ChangeTracker.Clear();
            // Edit mode
            if (FlowStep.Id > 0)
            {
                FlowStep updateFlowStep = await _dataService.FlowSteps.FirstAsync(x => x.Id == FlowStep.Id);
                updateFlowStep.Name = FlowStep.Name;
                updateFlowStep.Height = FlowStep.Height;
                updateFlowStep.Width = FlowStep.Width;
                updateFlowStep.ProcessName = FlowStep.ProcessName;
                updateFlowStep.Type = FlowStep.Type;
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
                    return;

                FlowStep.OrderingNum = isNewSimpling.OrderingNum;
                isNewSimpling.OrderingNum++;
                await _dataService.SaveChangesAsync();


                if (FlowStep.Name.Length == 0)
                    FlowStep.Name = "Set window size.";

                _dataService.FlowSteps.Add(FlowStep);
            }


            _dataService.SaveChanges();
            OnSave?.Invoke(FlowStep.Id);
        }
    }
}
