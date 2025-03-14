using Model.Models;
using StepinFlow.ViewModels.Pages;
using Wpf.Ui.Controls;
using StepinFlow.Views.UserControls;
using Wpf.Ui.Abstractions.Controls;

namespace StepinFlow.Views.Pages
{
    public partial class SubFlowsPage : INavigableView<SubFlowsVM>
    {
        public SubFlowsVM ViewModel { get; }

        public SubFlowsPage(SubFlowsVM viewModel, TreeViewUserControl treeViewUserControl)
        {
            ViewModel = viewModel;
            DataContext = this;
            InitializeComponent();

            ViewModel.TreeViewUserControl = TreeViewControl;
            ViewModel.FrameDetailUserControl = FrameDetailUserControl;
        }

        // FrameDetailUserControl.
        private void OnSaveFlow(object sender, int id) => ViewModel.OnSaveFlow(id);
        private void OnSaveFlowStep(object sender, int id) => ViewModel.OnSaveFlowStep(id);
        private void OnSaveFlowParameter(object sender, int id) => ViewModel.OnSaveFlowParameter(id);

        //TreeViewUserControl
        private void OnAddFlowStepClick(object sender, FlowStep newFlowStep) => ViewModel.OnAddFlowStepClick(newFlowStep);
        private void OnAddFlowParameterClick(object sender, FlowParameter newFlowParameter) => ViewModel.OnAddFlowParameterClick(newFlowParameter);
        private void OnFlowStepClone(object sender, int id) => ViewModel.OnFlowStepCopy(id);
        private async void OnSelectedFlowStepIdChange(object sender, int id) => ViewModel.OnTreeViewItemFlowStepSelected(id);
        private void OnSelectedFlowIdChange(object sender, int id) => ViewModel.OnTreeViewItemFlowSelected(id);
        private void OnSelectedFlowParameterIdChange(object sender, int id) => ViewModel.OnTreeViewItemFlowParameterSelected(id);
    }
}
