using CommunityToolkit.Mvvm.ComponentModel;
using Model.Models;
using Business.Interfaces;
using System.Collections.ObjectModel;
using Business.Services.Interfaces;

namespace StepinFlow.ViewModels.Pages.Executions
{
    public partial class GoToExecutionVM : ObservableObject, IExecutionViewModel
    {
        private readonly IDataService _dataService;

        [ObservableProperty]
        private Execution _execution;

        [ObservableProperty]
        private ObservableCollection<FlowStep> _previousSteps = new ObservableCollection<FlowStep>();

        public GoToExecutionVM(IDataService dataService)
        {
            _dataService = dataService;
            _execution = new Execution();
        }

        public Task SetExecution(Execution execution)
        {
            Execution = execution;
            return Task.CompletedTask;
        }
    }
}
