using CommunityToolkit.Mvvm.ComponentModel;
using Model.Models;
using Business.Interfaces;
using System.Collections.ObjectModel;
using Microsoft.EntityFrameworkCore;
using Business.Services.Interfaces;

namespace StepinFlow.ViewModels.Pages.Executions
{
    public partial class CursorRelocateExecutionVM : ObservableObject, IExecutionViewModel
    {
        private readonly IDataService _dataService;

        [ObservableProperty]
        private Execution _execution;

        [ObservableProperty]
        private ObservableCollection<FlowStep> _parents = new ObservableCollection<FlowStep>();

        public CursorRelocateExecutionVM(IDataService dataService)
        {
            _dataService = dataService;
            _execution = new Execution() { FlowStep = new FlowStep() };
        }

        public async Task SetExecution(Execution execution)
        {
            Parents.Clear();

            Execution = execution;
            //FlowStep? parentFloStep = await _dataService.FlowSteps.FirstOrDefaultAsync(x => x.Id == execution.FlowStep.ParentTemplateSearchFlowStepId.Value );
            //Parents.Add(parentFloStep);

        }
    }
}
