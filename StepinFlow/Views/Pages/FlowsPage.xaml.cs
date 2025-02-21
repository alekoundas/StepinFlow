﻿using Model.Models;
using StepinFlow.ViewModels.Pages;
using Wpf.Ui.Controls;
using StepinFlow.Views.UserControls;

namespace StepinFlow.Views.Pages
{
    public partial class FlowsPage : INavigableView<FlowsViewModel>
    {
        public FlowsViewModel ViewModel { get; }

        public FlowsPage(FlowsViewModel viewModel, TreeViewUserControl treeViewUserControl)
        {
            viewModel.LoadFlows += InvokeLoadFlowsAction;
            viewModel.ClearCopy += InvokeClearCopyAction;
            viewModel.ExpandAll += InvokeExpandAllAction;
            viewModel.CollapseAll += InvokeCollapseAllAction;

            viewModel.NavigateToFlow += InvokeNavigateToFlowAction;
            viewModel.NavigateToFlowStep += InvokeNavigateToFlowStepAction;
            viewModel.NavigateToNewFlowStep += InvokeNavigateToNewFlowStepAction;


            ViewModel = viewModel;
            DataContext = this;
            InitializeComponent();
        }


        private void OnAddFlowStepClick(object sender, FlowStep newFlowStep)
        {
            ViewModel.OnAddFlowStepClick(newFlowStep);
        }

        private void OnFlowStepClone(object sender, int id)
        {
            ViewModel.OnFlowStepCopy(id);
        }

        private void OnSelectedFlowStepIdChange(object sender, int id)
        {
            ViewModel.OnTreeViewItemFlowStepSelected(id);
        }

        private void OnSelectedFlowIdChange(object sender, int id)
        {
            ViewModel.OnTreeViewItemFlowSelected(id);
        }

        public async Task InvokeLoadFlowsAction(int? id = 0)
        {
            await TreeViewControl.ViewModel.LoadFlows(id);
        }

        public void InvokeClearCopyAction()
        {
            TreeViewControl.ViewModel.ClearCopy();
        }


        public async Task InvokeExpandAllAction()
        {
            await TreeViewControl.ViewModel.ExpandAll();
        }

        public async Task InvokeCollapseAllAction()
        {
            await TreeViewControl.ViewModel.CollapseAll();
        }

        public async Task InvokeNavigateToFlowAction(int id)
        {
            await FlowStepFrameUserControl.ViewModel.NavigateToFlow(id);
        }

        public async Task InvokeNavigateToFlowStepAction(int id)
        {
            await FlowStepFrameUserControl.ViewModel.NavigateToFlowStep(id);
        }

        public void InvokeNavigateToNewFlowStepAction(FlowStep flowStep)
        {
            FlowStepFrameUserControl.ViewModel.NavigateToNewFlowStep(flowStep);
        }
    }
}
