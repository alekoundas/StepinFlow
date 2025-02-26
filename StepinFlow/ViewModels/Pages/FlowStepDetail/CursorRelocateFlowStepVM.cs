﻿using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Model.Models;
using Business.Interfaces;
using DataAccess.Repository.Interface;
using System.Collections.ObjectModel;
using Model.Enums;
using Model.Structs;
using Business.BaseViewModels;

namespace StepinFlow.ViewModels.Pages
{
    public partial class CursorRelocateFlowStepVM : BaseFlowStepDetailVM
    {
        private readonly ISystemService _systemService;
        private readonly IDataService _dataService;
        public override event Action<int> OnSave;


        [ObservableProperty]
        private ObservableCollection<FlowStep> _parents = new ObservableCollection<FlowStep>();

        [ObservableProperty]
        private FlowStep? _selectedFlowStep = null;


        public CursorRelocateFlowStepVM(ISystemService systemService, IDataService dataService) : base(dataService)
        {
            _dataService = dataService;
            _systemService = systemService;
        }

        public override async Task LoadFlowStepId(int flowStepId)
        {
            FlowStep? flowStep = await _dataService.FlowSteps.FirstOrDefaultAsync(x => x.Id == flowStepId);
            if (flowStep != null)
            {
                FlowStep = flowStep;

                if (FlowStep.ParentTemplateSearchFlowStepId.HasValue)
                    GetParents(FlowStep.ParentTemplateSearchFlowStepId.Value);

                else if (FlowStep.ParentFlowStepId.HasValue)
                    GetParents(FlowStep.ParentFlowStepId.Value);

                SelectedFlowStep = Parents.FirstOrDefault(x => x.Id == flowStep.ParentTemplateSearchFlowStepId);
            }
        }

        public override async Task LoadNewFlowStep(FlowStep newFlowStep)
        {
            FlowStep = newFlowStep;

            if (FlowStep.ParentTemplateSearchFlowStepId.HasValue)
                GetParents(FlowStep.ParentTemplateSearchFlowStepId.Value);

            if (FlowStep.ParentFlowStepId.HasValue)
                GetParents(FlowStep.ParentFlowStepId.Value);

            return;
        }

        [RelayCommand]
        private void OnButtonTestClick()
        {
            Point point = new Point(FlowStep.LocationX, FlowStep.LocationY);
            _systemService.SetCursorPossition(point);
        }

        [RelayCommand]
        private void OnButtonCancelClick()
        {
            //TODO
        }

        [RelayCommand]
        private async Task OnButtonSaveClick()
        {
            _dataService.Query.ChangeTracker.Clear();
            // Edit mode
            if (FlowStep.Id > 0)
            {
                FlowStep updateFlowStep = await _dataService.FlowSteps.FirstAsync(x => x.Id == FlowStep.Id);
                updateFlowStep.Name = FlowStep.Name;

                if (SelectedFlowStep != null)
                    updateFlowStep.ParentTemplateSearchFlowStepId = SelectedFlowStep.Id;
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
                await _dataService.SaveChangesAsync();


                if (FlowStep.Name.Length == 0)
                    FlowStep.Name = "Set cursor possition.";

                if (SelectedFlowStep != null)
                    FlowStep.ParentTemplateSearchFlowStepId = SelectedFlowStep.Id;

                _dataService.FlowSteps.Add(FlowStep);
            }


            _dataService.SaveChanges();
            OnSave?.Invoke(FlowStep.Id);
        }

        private void GetParents(int? flowStepId)
        {
            Parents = new ObservableCollection<FlowStep>();
            if (!flowStepId.HasValue)
                return;

            FlowStep? parent = _dataService.FlowSteps.FirstOrDefault(x => x.Id == flowStepId.Value);

            while (parent != null)
            {
                if (parent.Type == FlowStepTypesEnum.TEMPLATE_SEARCH)
                    Parents.Add(parent);


                if (parent.Type == FlowStepTypesEnum.MULTIPLE_TEMPLATE_SEARCH)
                    Parents.Add(parent);


                if (parent.Type == FlowStepTypesEnum.WAIT_FOR_TEMPLATE)
                    Parents.Add(parent);

                if (!parent.ParentFlowStepId.HasValue)
                    return;

                parent = _dataService.FlowSteps.FirstOrDefault(x => x.Id == parent.ParentFlowStepId.Value);
            }
        }
    }
}
