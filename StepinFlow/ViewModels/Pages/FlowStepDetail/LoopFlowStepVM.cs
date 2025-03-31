using CommunityToolkit.Mvvm.ComponentModel;
using Model.Models;
using Business.Helpers;
using Model.Enums;
using System.Collections.ObjectModel;
using Business.BaseViewModels;
using Business.Services.Interfaces;
using Business.Factories.FormValidationFactory;
namespace StepinFlow.ViewModels.Pages
{
    public partial class LoopFlowStepVM : BaseFlowStepDetailVM
    {
        private readonly IDataService _dataService;
        private readonly IFormValidationFactory _formValidationFactory;
        //public override event Action<int> OnSave;

        [ObservableProperty]
        private List<string> _processList = SystemProcessHelper.GetProcessWindowTitles();

        [ObservableProperty]
        private string _templateImgPath = "";


        public LoopFlowStepVM(IDataService dataService, IFormValidationFactory formValidationFactory) : base(dataService)
        {
            _dataService = dataService;
            _formValidationFactory = formValidationFactory;
        }


        public override async Task LoadNewFlowStep(FlowStep newFlowStep)
        {
            ValidationHelper.ErrorsChanged += OnErrorsChange;
            FlowStep = newFlowStep;
            FlowStep.Name = "Loop.";
        }

        public override async Task<int> OnSave()
        {
            ValidationHelper.ClearErrors();
            _formValidationFactory.CreateValidator("FlowStep.Name").Validate(FlowStep.Name);
            _formValidationFactory.CreateValidator("FlowStep.LoopMaxCount").Validate(FlowStep.LoopMaxCount);

            if (ValidationHelper.HasErrors())
                return -1;



            // Edit mode
            if (FlowStep.Id > 0)
            {
                FlowStep updateFlowStep = await _dataService.FlowSteps.FirstAsync(x => x.Id == FlowStep.Id);
                updateFlowStep.Name = FlowStep.Name;
                updateFlowStep.LoopMaxCount = FlowStep.LoopMaxCount;
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

                // "Add" Flow step
                FlowStep newFlowStep = new FlowStep();
                newFlowStep.Type = FlowStepTypesEnum.NEW;

                FlowStep.ChildrenFlowSteps = new ObservableCollection<FlowStep> { newFlowStep };
                FlowStep.IsExpanded = true;

                await _dataService.FlowSteps.AddAsync(FlowStep);
            }
            return FlowStep.Id;
        }
    }
}

