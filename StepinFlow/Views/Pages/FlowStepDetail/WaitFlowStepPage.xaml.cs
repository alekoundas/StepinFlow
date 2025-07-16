using Business.Interfaces;
using StepinFlow.ViewModels.Pages;
using StepinFlow.Views.UserControls;
using System.Windows.Controls;

namespace StepinFlow.Views.Pages.FlowStepDetail
{
    public partial class WaitFlowStepPage : Page, IFlowStepDetailPage, IDetailPage
    {
        public IFlowDetailVM? FlowViewModel { get; set; }
        public IFlowStepDetailVM? FlowStepViewModel { get; set; }
        public IFlowParameterDetailVM? FlowParameterViewModel { get; set; }
        public IExecutionViewModel? FlowExecutionViewModel { get; set; }


        public IFlowStepDetailVM ViewModel { get; set; }
        public WaitFlowStepPage(WaitFlowStepVM viewModel)
        {

            ViewModel = viewModel;
            DataContext = this;
            FlowStepViewModel = ViewModel;
            InitializeComponent();

            viewModel.TimeSpanInputUserControl = TimeInputControl;
        }
    }
}
