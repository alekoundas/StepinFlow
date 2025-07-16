using Business.Interfaces;
using StepinFlow.ViewModels.Pages.Executions;
using System.Windows.Controls;

namespace StepinFlow.Views.Pages.Executions
{
    public partial class WaitExecutionPage : Page, IExecutionPage, IDetailPage
    {
        public IFlowDetailVM? FlowViewModel { get; set; }
        public IFlowStepDetailVM? FlowStepViewModel { get; set; }
        public IFlowParameterDetailVM? FlowParameterViewModel { get; set; }
        public IExecutionViewModel? FlowExecutionViewModel { get; set; }


        public IExecutionViewModel ViewModel { get; set; }
        public WaitExecutionPage(WaitExecutionVM viewModel)
        {
            ViewModel = viewModel;
            DataContext = this;
            FlowExecutionViewModel = ViewModel;
            InitializeComponent();

            viewModel.TimeSpanInputUserControl = TimeInputControl;

        }
    }
}
