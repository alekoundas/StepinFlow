using CommunityToolkit.Mvvm.ComponentModel;
using Model.Models;
using Model.Enums;
using Business.Interfaces;

namespace StepinFlow.ViewModels.Pages.Executions
{
    public partial class CursorClickExecutionVM : ObservableObject, IExecutionViewModel
    {
        [ObservableProperty]
        private Execution _execution;

        [ObservableProperty]
        private IEnumerable<MouseButtonsEnum> _mouseButtonsEnum;


        [ObservableProperty]
        private IEnumerable<MouseActionsEnum> _mouseActionsEnum;
        public CursorClickExecutionVM() 
        {
            _execution = new Execution() { FlowStep = new FlowStep() };
            MouseButtonsEnum = Enum.GetValues(typeof(MouseButtonsEnum)).Cast<MouseButtonsEnum>();
            MouseActionsEnum = Enum.GetValues(typeof(MouseActionsEnum)).Cast<MouseActionsEnum>();
        }

        public Task SetExecution(Execution execution)
        {
            Execution = execution;
            return Task.CompletedTask;
        }
    }
}
