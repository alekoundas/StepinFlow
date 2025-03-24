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
        private IEnumerable<CursorButtonsEnum> _mouseButtonsEnum;

        [ObservableProperty]
        private IEnumerable<CursorActionsEnum> _mouseActionsEnum;

        public CursorScrollExecutionVM() 
        {
            _execution = new Execution();

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
