using CommunityToolkit.Mvvm.ComponentModel;
using Model.Models;
using Model.Enums;
using System.Collections.ObjectModel;
using Business.BaseViewModels;
using Business.Services.Interfaces;
using Business.Helpers;
using Business.Factories.FormValidationFactory;

namespace StepinFlow.ViewModels.Pages
{
    public partial class GoToFlowStepVM : BaseFlowStepDetailVM
    {
        private readonly IDataService _dataService;
        private readonly IFormValidationFactory _formValidationFactory;

        [ObservableProperty]
        private ObservableCollection<FlowStep> _previousSteps = new ObservableCollection<FlowStep>();


        public GoToFlowStepVM(IDataService dataService, IFormValidationFactory formValidationFactory) : base(dataService)
        {
            _dataService = dataService;
            _formValidationFactory = formValidationFactory;
        }

        public override async Task LoadFlowStepId(int flowStepId)
        {
            ValidationHelper.ErrorsChanged += OnErrorsChange;
            FlowStep? flowStep = await _dataService.FlowSteps.FirstOrDefaultAsync(x => x.Id == flowStepId);
            if (flowStep != null)
            {
                FlowStep = flowStep;
                if (flowStep.ParentFlowStepId != null)
                    PreviousSteps = await GetParents(flowStep.ParentFlowStepId.Value);
            }
        }

        public override async Task LoadNewFlowStep(FlowStep newFlowStep)
        {
            ValidationHelper.ErrorsChanged += OnErrorsChange;
            FlowStep = newFlowStep;
            FlowStep.Name = "Go to step.";

            if (newFlowStep.ParentFlowStepId != null)
                PreviousSteps = await GetParents(newFlowStep.ParentFlowStepId.Value);
        }



        public override async Task<int> OnSave()
        {
            ValidationHelper.ClearErrors();
            _formValidationFactory.CreateValidator("FlowStep.Name").Validate(FlowStep.Name);
            _formValidationFactory.CreateValidator("FlowStep.ParentTemplateSearchFlowStep").Validate(FlowStep.ParentTemplateSearchFlowStep);

            if (ValidationHelper.HasErrors())
                return -1;


            // Edit mode
            if (FlowStep.Id > 0)
            {

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

        private async Task<ObservableCollection<FlowStep>> GetParents(int flowStepId)
        {
            List<FlowStep> previousSteps = new List<FlowStep>();

            FlowStep? parent = _dataService.FlowSteps
                .Include(x => x.ParentFlowStep)
                .FirstOrDefault(x => x.Id == flowStepId);

            while (parent != null)
            {

                List<FlowStep> siblings = await _dataService.FlowSteps.GetSiblings(parent.Id);
                siblings = siblings
                    .Where(x => x.OrderingNum < parent.OrderingNum)
                    .Where(x => x.Type != FlowStepTypesEnum.NEW)
                    .Where(x => x.Type != FlowStepTypesEnum.SUCCESS)
                    .Where(x => x.Type != FlowStepTypesEnum.FAILURE)
                    .Where(x => x.Type != FlowStepTypesEnum.FLOW_STEPS)
                    .Where(x => x.Type != FlowStepTypesEnum.FLOW_PARAMETERS)
                    .Where(x => x.Type != FlowStepTypesEnum.MULTIPLE_TEMPLATE_SEARCH_CHILD)
                    .OrderByDescending(x => x.OrderingNum)
                    .ToList();

                previousSteps.AddRange(siblings);

                //Get parent flowStep
                if (parent?.ParentFlowStepId != null)
                    parent = _dataService.FlowSteps
                        .Include(x => x.ParentFlowStep)
                        .FirstOrDefault(x => x.Id == parent.ParentFlowStepId);

                //Get parent SubflowStep
                else if (parent?.FlowId != null)
                    parent = _dataService.Flows
                        .Where(x => x.Id == parent.FlowId)
                        .Select<FlowStep?>(x => x.ParentSubFlowStep)
                        .FirstOrDefault();
                else
                    parent = null;
            }



            return new ObservableCollection<FlowStep>(previousSteps);
        }
    }
}
