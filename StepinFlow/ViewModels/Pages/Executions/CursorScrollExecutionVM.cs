using CommunityToolkit.Mvvm.ComponentModel;
using Model.Models;
using Business.Interfaces;
using Model.Enums;

namespace StepinFlow.ViewModels.Pages.Executions
{
    public partial class CursorScrollExecutionVM : ObservableObject, IExecutionViewModel
    {
        [ObservableProperty]
        private Execution _execution;

        [ObservableProperty]
        private IEnumerable<MouseButtonsEnum> _mouseButtonsEnum;

        [ObservableProperty]
        private IEnumerable<MouseActionsEnum> _mouseActionsEnum;

        public CursorScrollExecutionVM() 
        {
            _execution = new Execution();

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
