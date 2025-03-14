using Model.Models;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using Model.Enums;
using Wpf.Ui.Abstractions.Controls;
using Business.Services.Interfaces;
using StepinFlow.Views.UserControls;

namespace StepinFlow.ViewModels.Pages
{
    public partial class SubFlowsVM : ObservableObject, INavigationAware, INotifyPropertyChanged
    {
        private readonly IDataService _dataService;
        public TreeViewUserControl TreeViewUserControl;
        public FrameDetailUserControl FrameDetailUserControl;



        [ObservableProperty]
        private bool _isLocked = true;
        [ObservableProperty]
        private Visibility _visibleAddFlow = Visibility.Collapsed;

        [ObservableProperty]
        private int? _coppiedFlowStepId = null;
        [ObservableProperty]
        private int? _coppiedFlowId = null;
        [ObservableProperty]
        private string? _coppiedDisplayText = "";
        [ObservableProperty]
        private Visibility _visible = Visibility.Collapsed;

        public SubFlowsVM(IDataService dataService)
        {
            _dataService = dataService;
        }

        // TreeViewUserControl.
        public async Task RefreshData()
        {
            await TreeViewUserControl.ViewModel.LoadFlows(-1, true);
        }
        public async Task OnSaveFlow(int id)
        {
            await TreeViewUserControl.ViewModel.LoadFlowsAndSelectFlow(id);
        }
        public async Task OnSaveFlowStep(int id)
        {
            await TreeViewUserControl.ViewModel.LoadFlowsAndSelectFlowStep(id);
        }
        public async Task OnSaveFlowParameter(int id)
        {
            await TreeViewUserControl.ViewModel.LoadFlowsAndSelectFlowParameter(id);
        }


        // FrameDetailUserControl.
        public void OnAddFlowStepClick(FlowStep newFlowStep)
        {
            FrameDetailUserControl.ViewModel.NavigateToNewFlowStep(newFlowStep);
        }
        public void OnAddFlowParameterClick(FlowParameter newFlowParameter)
        {
            FrameDetailUserControl.ViewModel.NavigateToNewFlowParameter(newFlowParameter);
        }

        public async Task OnTreeViewItemFlowStepSelected(int id)
        {
            await FrameDetailUserControl.ViewModel.NavigateToFlowStep(id);
        }

        public async Task OnTreeViewItemFlowSelected(int id)
        {
            await FrameDetailUserControl.ViewModel.NavigateToFlow(id);
        }
        public async Task OnTreeViewItemFlowParameterSelected(int id)
        {
            await FrameDetailUserControl.ViewModel.NavigateToFlowParameter(id);
        }

        public void OnFlowStepCopy(int id)
        {
            CoppiedFlowStepId = id;
            CoppiedDisplayText = "Coppied FlowStep ID: ";
            Visible = Visibility.Visible;
        }


        [RelayCommand]
        private void OnButtonClearCopyClick()
        {
            CoppiedFlowStepId = null;
            CoppiedFlowId = null;
            Visible = Visibility.Collapsed;
            TreeViewUserControl.ViewModel.ClearCopy();
        }

        [RelayCommand]
        private async Task OnButtonAddFlowClick()
        {
            FlowParameter flowRarameter = new FlowParameter
            {
                Name = "Flow parameters.",
                Type = FlowParameterTypesEnum.FLOW_PARAMETERS,
                ChildrenFlowParameters = new ObservableCollection<FlowParameter>() { new FlowParameter { Type = FlowParameterTypesEnum.NEW } }
            };

            FlowStep flowSteps = new FlowStep
            {
                Name = "Flow steps.",
                Type = FlowStepTypesEnum.FLOW_STEPS,
                ChildrenFlowSteps = new ObservableCollection<FlowStep>() { new FlowStep { Type = FlowStepTypesEnum.NEW } }
            };

            Flow flow = new Flow
            {
                Name = "Sub-Flow",
                IsSelected = true,
                FlowStep = flowSteps,
                FlowParameter = flowRarameter,
                Type = FlowTypesEnum.SUB_FLOW
            };

            await _dataService.Flows.AddAsync(flow);

            flow.FlowStepId = flowSteps.Id;
            flow.FlowParameterId = flowRarameter.Id;
            await _dataService.UpdateAsync(flow);

            await RefreshData();
        }


        [RelayCommand]
        private void OnButtonLockClick()
        {
            IsLocked = !IsLocked;

            if (IsLocked)
                VisibleAddFlow = Visibility.Collapsed;
            else
                VisibleAddFlow = Visibility.Visible;

            TreeViewUserControl.ViewModel.IsLocked = IsLocked;
        }

        [RelayCommand]
        private async Task OnButtonSyncClick()
        {
            await RefreshData();
        }

        [RelayCommand]
        private async Task OnButtonExpandAllClick()
        {
            await TreeViewUserControl.ViewModel.ExpandAll();
        }

        [RelayCommand]
        private async Task OnButtonCollapseAllClick()
        {
            await TreeViewUserControl.ViewModel.CollapseAll();
        }




        public async Task OnNavigatedToAsync()
        {
            await RefreshData();
        }

        public Task OnNavigatedFromAsync()
        {
            return Task.CompletedTask;
        }
    }
}
