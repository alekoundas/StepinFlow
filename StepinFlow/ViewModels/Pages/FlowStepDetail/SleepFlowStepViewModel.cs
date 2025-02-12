﻿using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Model.Models;
using Business.Interfaces;
using DataAccess.Repository.Interface;

namespace StepinFlow.ViewModels.Pages
{
    public partial class SleepFlowStepViewModel : ObservableObject, IFlowStepViewModel
    {
        private readonly ISystemService _systemService;
        private readonly IBaseDatawork _baseDatawork;
        private readonly FlowsViewModel _flowsViewModel;

        [ObservableProperty]
        private FlowStep _flowStep = new FlowStep();

        [ObservableProperty]
        private string _timeTotal;
        public SleepFlowStepViewModel( FlowsViewModel flowsViewModel, ISystemService systemService, IBaseDatawork baseDatawork)
        {

            _baseDatawork = baseDatawork;
            _systemService = systemService;
            _flowsViewModel = flowsViewModel;

            int miliseconds = 0;
            miliseconds += FlowStep.SleepForMilliseconds;
            miliseconds += FlowStep.SleepForSeconds * 1000;
            miliseconds += FlowStep.SleepForMinutes * 60 * 1000;
            miliseconds += FlowStep.SleepForHours * 60 * 60 * 1000;

            TimeTotal = TimeSpan.FromMilliseconds(miliseconds).ToString(@"hh\:mm\:ss");
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
                updateFlowStep.SleepForHours = FlowStep.SleepForHours;
                updateFlowStep.SleepForMinutes = FlowStep.SleepForMinutes;
                updateFlowStep.SleepForSeconds = FlowStep.SleepForSeconds;
                updateFlowStep.SleepForMilliseconds = FlowStep.SleepForMilliseconds;

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
                    FlowStep.Name = "Wait";

                _baseDatawork.FlowSteps.Add(FlowStep);
            }


            _baseDatawork.SaveChanges();
            await _flowsViewModel.RefreshData();
        }
    }
}
