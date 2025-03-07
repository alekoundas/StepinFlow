using CommunityToolkit.Mvvm.ComponentModel;
using Model.Models;
using Business.Interfaces;
using Business.Helpers;

namespace StepinFlow.ViewModels.Pages.Executions
{
    public partial class WindowMoveExecutionVM : ObservableObject, IExecutionViewModel
    {
        [ObservableProperty]
        private Execution _execution;

        [ObservableProperty]
        private List<string> _processList = SystemProcessHelper.GetProcessWindowTitles();

        public WindowMoveExecutionVM()
        {
            _execution = new Execution() { FlowStep = new FlowStep() };
        }

        public Task SetExecution(Execution execution)
        {
            Execution = execution;
            return Task.CompletedTask;
        }
    }
}
