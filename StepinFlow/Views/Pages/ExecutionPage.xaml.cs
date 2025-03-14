using StepinFlow.ViewModels.Pages;
using Wpf.Ui.Abstractions.Controls;

namespace StepinFlow.Views.Pages
{
    public partial class ExecutionPage : INavigableView<ExecutionVM>
    {
        public ExecutionVM ViewModel { get; }
        public ExecutionPage(ExecutionVM viewModel)
        {
            ViewModel = viewModel;
            DataContext = this;
            InitializeComponent();

            ViewModel.TreeViewUserControl = TreeViewControl;
            ViewModel.FrameDetailUserControl= FrameDetailUserControl;
        }
    }
}
