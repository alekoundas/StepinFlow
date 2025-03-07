using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Model.Models;
using Model.Enums;
using Business.BaseViewModels;
using Business.Services.Interfaces;

namespace StepinFlow.ViewModels.Pages
{
    public partial class CursorClickFlowStepVM : BaseFlowStepDetailVM
    {
        private readonly IDataService _dataService;
        //public override event Action<int> OnSave;

        [ObservableProperty]
        private IEnumerable<MouseButtonsEnum> _mouseButtonsEnum;
        [ObservableProperty]
        private IEnumerable<MouseActionsEnum> _mouseActionsEnum;


        public CursorClickFlowStepVM(IDataService dataService) : base(dataService)
        {
            _dataService = dataService;

            MouseButtonsEnum = Enum.GetValues(typeof(MouseButtonsEnum)).Cast<MouseButtonsEnum>();
            MouseActionsEnum = Enum.GetValues(typeof(MouseActionsEnum)).Cast<MouseActionsEnum>();
        }


        public override async Task OnSave()
        {
            // Edit mode
            if (FlowStep.Id > 0)
            {
                FlowStep updateFlowStep = await _dataService.FlowSteps.FirstAsync(x => x.Id == FlowStep.Id);
                updateFlowStep.Name = FlowStep.Name;
                updateFlowStep.CursorAction = FlowStep.CursorAction;
                updateFlowStep.CursorButton = FlowStep.CursorButton;
                await _dataService.UpdateAsync(updateFlowStep);
            }

            /// Add mode
            else
            {
                FlowStep isNewSimpling;

                if (FlowStep.ParentFlowStepId != null)
                    isNewSimpling = await _dataService.FlowSteps.GetIsNewSibling(FlowStep.ParentFlowStepId.Value);
                else if (FlowStep.FlowId.HasValue)
                    isNewSimpling = await _dataService.Flows.GetIsNewSibling(FlowStep.FlowId.Value);
                else
                    return;

                FlowStep.OrderingNum = isNewSimpling.OrderingNum;
                isNewSimpling.OrderingNum++;
                await _dataService.UpdateAsync(isNewSimpling);


                if (FlowStep.Name.Length == 0)
                    FlowStep.Name = "Set cursor Action.";

                await _dataService.FlowSteps.AddAsync(FlowStep);
            }
        }
    }
}
