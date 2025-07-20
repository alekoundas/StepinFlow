using Business.BaseViewModels;
using Business.Helpers;
using Business.Services.Interfaces;
using Model.Models;
using StepinFlow.Views.UserControls;

namespace StepinFlow.ViewModels.Pages
{
    public partial class WaitFlowStepVM : BaseFlowStepDetailVM
    {
        private readonly ISystemService _systemService;
        private readonly IDataService _dataService;

        public TimeSpanInputUserControl TimeSpanInputUserControl;

        public WaitFlowStepVM(ISystemService systemService, IDataService dataService) : base(dataService)
        {
            _dataService = dataService;
            _systemService = systemService;
        }

        public override async Task LoadNewFlowStep(FlowStep newFlowStep)
        {
            ValidationHelper.ErrorsChanged += OnErrorsChange;
            FlowStep = newFlowStep;
            FlowStep.Name = "Wait";
        }

        public override async Task LoadFlowStepId(int flowStepId)
        {
            ValidationHelper.ErrorsChanged += OnErrorsChange;
            FlowStep? flowStep = await _dataService.FlowSteps.FirstOrDefaultAsync(x => x.Id == flowStepId);
            if (flowStep != null)
                FlowStep = flowStep;

            TimeSpanInputUserControl.ViewModel.SetFromTotalMilliseconds(FlowStep.Milliseconds);
        }

        public override async Task<int> OnSave()
        {

            // Edit mode
            if (FlowStep.Id > 0)
            {
                FlowStep updateFlowStep = await _dataService.FlowSteps.FirstAsync(x => x.Id == FlowStep.Id);
                updateFlowStep.Name = FlowStep.Name;
                updateFlowStep.Milliseconds = TimeSpanInputUserControl.ViewModel.TotalMilliseconds;
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


                FlowStep.Milliseconds = TimeSpanInputUserControl.ViewModel.TotalMilliseconds;
                await _dataService.FlowSteps.AddAsync(FlowStep);
            }
            return FlowStep.Id;
        }
    }
}
