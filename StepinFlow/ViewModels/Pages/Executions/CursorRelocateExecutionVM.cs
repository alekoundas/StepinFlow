using CommunityToolkit.Mvvm.ComponentModel;
using Model.Models;
using Business.Interfaces;
using System.Collections.ObjectModel;
using Business.Services.Interfaces;
using Model.Enums;
using CommunityToolkit.Mvvm.Input;
using Model.Structs;

namespace StepinFlow.ViewModels.Pages.Executions
{
    public partial class CursorRelocateExecutionVM : ObservableObject, IExecutionViewModel
    {
        private readonly IDataService _dataService;
        private readonly ISystemService _systemService;

        [ObservableProperty]
        private Execution _execution;

        [ObservableProperty]
        private IEnumerable<CursorRelocationTypesEnum> _cursorRelocationTypesEnum;

        [ObservableProperty]
        private ObservableCollection<FlowStep> _parents = new ObservableCollection<FlowStep>();
        [ObservableProperty]
        private FlowStep? _selectedFlowStep = null;

        public CursorRelocateExecutionVM(IDataService dataService, ISystemService systemService)
        {
            _dataService = dataService;
            _execution = new Execution() { FlowStep = new FlowStep() };

            CursorRelocationTypesEnum = Enum.GetValues(typeof(CursorRelocationTypesEnum)).Cast<CursorRelocationTypesEnum>();
            _systemService = systemService;
        }

        public async Task SetExecution(Execution execution)
        {
            Parents.Clear();
            Execution = execution;


            if (execution?.FlowStep?.FlowParameter != null)
            {
                Parents.Add(execution.FlowStep.ParentTemplateSearchFlowStep);
                SelectedFlowStep = execution.FlowStep.ParentTemplateSearchFlowStep;
            }
        }

        [RelayCommand]
        private void OnButtonTestClick()
        {
            Point point = new Point(Execution.FlowStep.LocationX, Execution.FlowStep.LocationY);
            _systemService.SetCursorPossition(point);
        }
    }
}
