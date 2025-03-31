using Business.Helpers;
using Business.Interfaces;
using Business.Services.Interfaces;
using CommunityToolkit.Mvvm.ComponentModel;
using Model.Models;

namespace Business.BaseViewModels
{
    public partial class BaseFlowStepDetailVM : ObservableObject, IFlowStepDetailVM
    {
        private readonly IDataService _dataService;

        [ObservableProperty]
        protected Dictionary<string, string> _validationErrors = new Dictionary<string, string>();

        [ObservableProperty]
        protected FlowStep _flowStep = new FlowStep();

        public BaseFlowStepDetailVM(IDataService dataService)
        {
            _dataService = dataService;
        }

        public virtual async Task LoadFlowStepId(int flowStepId)
        {
            ValidationHelper.ErrorsChanged += OnErrorsChange;
            FlowStep? flowStep = await _dataService.FlowSteps.FirstOrDefaultAsync(x => x.Id == flowStepId);
            if (flowStep != null)
                FlowStep = flowStep;
        }

        public virtual Task LoadNewFlowStep(FlowStep newFlowStep)
        {
            ValidationHelper.ErrorsChanged += OnErrorsChange;
            FlowStep = newFlowStep;
            return Task.CompletedTask;
        }

        public int GetCurrentEntityId()
        {
            return FlowStep.Id;
        }

        public bool IsFormValid()
        {
            return !ValidationHelper.HasErrors();
        }


        public virtual async Task OnCancel()
        {
            if (FlowStep.Id == 0)
                FlowStep = new FlowStep();
            else
                await LoadFlowStepId(FlowStep.Id);

            ValidationHelper.ClearErrors();
        }

        public virtual void OnPageExit()
        {
            ValidationHelper.ErrorsChanged -= OnErrorsChange;
            ValidationHelper.ClearErrors();
        }

        public FlowStep GetFlowStep()
        {
            return FlowStep;
        }

        public virtual Task<int> OnSave()
        {
            throw new NotImplementedException();
        }

        //TODO private
        public void OnErrorsChange(object? sender, string propertyPath)
        {
            ValidationErrors = ValidationHelper.GetAllErrors();
        }
    }
}
