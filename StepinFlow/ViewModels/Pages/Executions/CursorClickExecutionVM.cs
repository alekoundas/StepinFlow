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
        private IEnumerable<CursorButtonsEnum> _mouseButtonsEnum;


        [ObservableProperty]
        private IEnumerable<CursorActionsEnum> _mouseActionsEnum;
        public CursorClickExecutionVM() 
        {
            _execution = new Execution() { FlowStep = new FlowStep() };
            MouseButtonsEnum = Enum.GetValues(typeof(CursorButtonsEnum)).Cast<CursorButtonsEnum>();
            MouseActionsEnum = Enum.GetValues(typeof(CursorActionsEnum)).Cast<CursorActionsEnum>();
        }

        public Task SetExecution(Execution execution)
        {
            Execution = execution;
            return Task.CompletedTask;
        }
    }
}
