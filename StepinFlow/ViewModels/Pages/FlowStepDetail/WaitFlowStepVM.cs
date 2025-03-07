using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Model.Models;
using Business.BaseViewModels;
using Business.Services.Interfaces;

namespace StepinFlow.ViewModels.Pages
{
    public partial class WaitFlowStepVM : BaseFlowStepDetailVM
    {
        private readonly ISystemService _systemService;
        private readonly IDataService _dataService;
        //public override event Action<int> OnSave;

        [ObservableProperty]
        private string _timeTotal;
        public WaitFlowStepVM(ISystemService systemService, IDataService dataService) : base(dataService)
        {

            _dataService = dataService;
            _systemService = systemService;


            int miliseconds = 0;
            miliseconds += FlowStep.WaitForMilliseconds;
            miliseconds += FlowStep.WaitForSeconds * 1000;
            miliseconds += FlowStep.WaitForMinutes * 60 * 1000;
            miliseconds += FlowStep.WaitForHours * 60 * 60 * 1000;

            TimeTotal = TimeSpan.FromMilliseconds(miliseconds).ToString(@"hh\:mm\:ss");
        }
        public override void OnPageExit()
        {
            //TestResultImage = null;
        }
        public override async Task OnSave()
        {
            // Edit mode
            if (FlowStep.Id > 0)
            {
                FlowStep updateFlowStep = await _dataService.FlowSteps.FirstAsync(x => x.Id == FlowStep.Id);
                updateFlowStep.Name = FlowStep.Name;
                updateFlowStep.WaitForHours = FlowStep.WaitForHours;
                updateFlowStep.WaitForMinutes = FlowStep.WaitForMinutes;
                updateFlowStep.WaitForSeconds = FlowStep.WaitForSeconds;
                updateFlowStep.WaitForMilliseconds = FlowStep.WaitForMilliseconds;
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
                    FlowStep.Name = "Wait";

                await _dataService.FlowSteps.AddAsync(FlowStep);
            }


            //OnSave?.Invoke(FlowStep.Id);
        }
    }
}
