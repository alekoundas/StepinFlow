﻿using CommunityToolkit.Mvvm.ComponentModel;
using Model.Models;
using Model.Enums;
using Business.BaseViewModels;
using System.Collections.ObjectModel;
using Business.Services.Interfaces;

namespace StepinFlow.ViewModels.Pages
{
    public partial class SubFlowStepVM : BaseFlowStepDetailVM
    {
        private readonly IDataService _dataService;
        private readonly ICloneService _cloneService;

        [ObservableProperty]
        private bool _isEnabled;
        [ObservableProperty]
        private ObservableCollection<Flow> _subFlows = new ObservableCollection<Flow>();
        [ObservableProperty]
        private Flow? _selectedSubFlow = null;


        public SubFlowStepVM(IDataService dataService, ICloneService cloneService) : base(dataService)
        {
            _dataService = dataService;
            _cloneService = cloneService;
        }


        public override async Task LoadFlowStepId(int flowStepId)
        {
            IsEnabled = false;
            SubFlows = new ObservableCollection<Flow>(await _dataService.Flows.Where(x => x.Type == FlowTypesEnum.SUB_FLOW).ToListAsync());

            FlowStep? flowStep = await _dataService.FlowSteps.FirstOrDefaultAsync(x => x.Id == flowStepId);
            if (flowStep != null)
                FlowStep = flowStep;

            if (FlowStep.SubFlowId.HasValue)
                SelectedSubFlow = SubFlows.FirstOrDefault(x => x.Id == FlowStep.SubFlowId);

        }

        public override async Task LoadNewFlowStep(FlowStep newFlowStep)
        {
            SubFlows = new ObservableCollection<Flow>(await _dataService.Flows.Where(x => x.Type == FlowTypesEnum.SUB_FLOW).ToListAsync());
            FlowStep = newFlowStep;
            IsEnabled = true;
            FlowStep.Name = "Sub-Flow.";
        }

        public override void OnPageExit()
        {
            SelectedSubFlow = null;
        }

        public override async Task<int> OnSave()
        {
            // Edit is disabled.
            // Edit mode
            if (FlowStep.Id > 0)
            {
                FlowStep updateFlowStep = await _dataService.FlowSteps.FirstAsync(x => x.Id == FlowStep.Id);
                updateFlowStep.Name = FlowStep.Name;
                updateFlowStep.IsSubFlowReferenced = FlowStep.IsSubFlowReferenced;
                updateFlowStep.SubFlowId = SelectedSubFlow?.Id;
                await _dataService.UpdateAsync(updateFlowStep);
            }

            // Add mode
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

                FlowStep.IsExpanded = false;

                if (FlowStep.IsSubFlowReferenced)
                {
                    FlowStep.SubFlowId = SelectedSubFlow?.Id;
                    _dataService.FlowSteps.Add(FlowStep);

                }
                else if (SelectedSubFlow?.Id != null)
                {
                    Flow? flow = await _cloneService.GetFlowClone(SelectedSubFlow.Id);
                    if (flow == null)
                        return -1;

                    FlowStep.SubFlow = flow;

                    await _dataService.FlowSteps.AddAsync(FlowStep);

                    flow.ParentSubFlowStepId = FlowStep.Id;
                    await _dataService.UpdateAsync(flow);
                }
            }
            return FlowStep.Id;
        }
    }
}
