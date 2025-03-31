using CommunityToolkit.Mvvm.ComponentModel;
using Model.Models;
using Model.Enums;
using Business.BaseViewModels;
using Business.Services.Interfaces;
using Business.Helpers;
using Business.Factories.FormValidationFactory;

namespace StepinFlow.ViewModels.Pages
{
    public partial class CursorClickFlowStepVM : BaseFlowStepDetailVM
    {
        private readonly IDataService _dataService;
        private readonly IFormValidationFactory _formValidationFactory;
        //public override event Action<int> OnSave;

        [ObservableProperty]
        private IEnumerable<CursorButtonsEnum> _mouseButtonsEnum;
        [ObservableProperty]
        private IEnumerable<CursorActionsEnum> _mouseActionsEnum;

        public CursorClickFlowStepVM(IDataService dataService, IFormValidationFactory formValidationFactory) : base(dataService)
        {
            _dataService = dataService;
            _formValidationFactory = formValidationFactory;

            MouseButtonsEnum = Enum.GetValues(typeof(CursorButtonsEnum)).Cast<CursorButtonsEnum>();
            MouseActionsEnum = Enum.GetValues(typeof(CursorActionsEnum)).Cast<CursorActionsEnum>();
        }

        public override Task LoadNewFlowStep(FlowStep newFlowStep)
        {
            ValidationHelper.ErrorsChanged += OnErrorsChange;
            FlowStep = newFlowStep;
            FlowStep.Name = "Cursor click.";

            return Task.CompletedTask;
        }


        public override async Task<int> OnSave()
        {
            ValidationHelper.ClearErrors();
            _formValidationFactory.CreateValidator("FlowStep.Name").Validate(FlowStep.Name);
            _formValidationFactory.CreateValidator("FlowStep.CursorAction").Validate(FlowStep.CursorAction);
            _formValidationFactory.CreateValidator("FlowStep.CursorButton").Validate(FlowStep.CursorButton);

            if (ValidationHelper.HasErrors())
                return -1;

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
                    return -1;

                FlowStep.OrderingNum = isNewSimpling.OrderingNum;
                isNewSimpling.OrderingNum++;
                await _dataService.UpdateAsync(isNewSimpling);

                await _dataService.FlowSteps.AddAsync(FlowStep);
            }

            return FlowStep.Id;
        }
    }
}
