﻿using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Model.Models;
using System.Collections.ObjectModel;
using Model.Enums;
using Model.Structs;
using Business.BaseViewModels;
using Business.Services;
using System.Windows.Input;
using Business.Services.Interfaces;
using Business.Helpers;
using Business.Factories.FormValidationFactory;

namespace StepinFlow.ViewModels.Pages
{
    public partial class CursorRelocateFlowStepVM : BaseFlowStepDetailVM
    {
        private readonly IDataService _dataService;
        private readonly ISystemService _systemService;
        private readonly IKeyboardListenerService _keyboardListenerService;
        private readonly IFormValidationFactory _formValidationFactory;
        //public override event Action<int> OnSave;


        [ObservableProperty]
        private ObservableCollection<FlowStep> _parents = new ObservableCollection<FlowStep>();

        [ObservableProperty]
        private IEnumerable<CursorRelocationTypesEnum> _cursorRelocationTypes;

        public CursorRelocateFlowStepVM(
            IDataService dataService,
            ISystemService systemService,
            IKeyboardListenerService keyboardListenerService,
            IFormValidationFactory formValidationFactory) : base(dataService)
        {
            _dataService = dataService;
            _systemService = systemService;
            _keyboardListenerService = keyboardListenerService;
            _formValidationFactory = formValidationFactory;

            CursorRelocationTypes = Enum.GetValues(typeof(CursorRelocationTypesEnum)).Cast<CursorRelocationTypesEnum>();
        }

        public override async Task LoadFlowStepId(int flowStepId)
        {
            ValidationHelper.ErrorsChanged += OnErrorsChange;
            FlowStep? flowStep = await _dataService.FlowSteps.FirstOrDefaultAsync(x => x.Id == flowStepId);
            if (flowStep != null)
            {
                FlowStep = flowStep;
                if (FlowStep.ParentFlowStepId.HasValue)
                    Parents = new ObservableCollection<FlowStep>(GetParents(FlowStep.ParentFlowStepId.Value));


                KeyCombination _combination = new KeyCombination(ModifierKeys.None, Key.F3);
                _keyboardListenerService.RegisterListener(_combination, () =>
                {
                    OnButtonRecordClick();
                });
            }
        }

        public override async Task LoadNewFlowStep(FlowStep newFlowStep)
        {
            ValidationHelper.ErrorsChanged += OnErrorsChange;
            FlowStep = newFlowStep;

            if (FlowStep.ParentFlowStepId.HasValue)
                Parents = new ObservableCollection<FlowStep>(GetParents(FlowStep.ParentFlowStepId.Value));


            KeyCombination _combination = new KeyCombination(ModifierKeys.None, Key.F3);
            _keyboardListenerService.RegisterListener(_combination, () =>
            {
                OnButtonRecordClick();
            });

            FlowStep.Name = "Cursor relocate.";
            return;
        }

        public override void OnPageExit()
        {
            base.OnPageExit();
            _keyboardListenerService.UnregisterAllListeners();
        }



        [RelayCommand]
        private void OnButtonTestClick()
        {
            Point point = new Point(FlowStep.LocationX, FlowStep.LocationY);
            _systemService.SetCursorPossition(point);
        }


        [RelayCommand]
        private void OnButtonRecordClick()
        {
            var point = _systemService.GetCursorPossition();
            FlowStep.LocationY = point.Y;
            FlowStep.LocationX = point.X;
        }

        public override async Task<int> OnSave()
        {
            ValidationHelper.ClearErrors();
            _formValidationFactory.CreateValidator("FlowStep.Name").Validate(FlowStep.Name);
            _formValidationFactory.CreateValidator("FlowStep.CursorRelocationType").Validate(FlowStep.CursorRelocationType);
            if (FlowStep.CursorRelocationType == CursorRelocationTypesEnum.CUSTOM)
            {
                _formValidationFactory.CreateValidator("FlowStep.LocationX").Validate(FlowStep.LocationX);
                _formValidationFactory.CreateValidator("FlowStep.LocationY").Validate(FlowStep.LocationY);
            }

            if (FlowStep.CursorRelocationType == CursorRelocationTypesEnum.USE_PARENT_RESULT)
            {
                _formValidationFactory.CreateValidator("FlowStep.ParentTemplateSearchFlowStep").Validate(FlowStep.ParentTemplateSearchFlowStep);
            }


            if (ValidationHelper.HasErrors())
                return -1;


            // Edit mode
            if (FlowStep.Id > 0)
            {
                FlowStep updateFlowStep = await _dataService.FlowSteps.FirstAsync(x => x.Id == FlowStep.Id);
                updateFlowStep.Name = FlowStep.Name;
                updateFlowStep.LocationX = FlowStep.LocationX;
                updateFlowStep.LocationY = FlowStep.LocationY;
                updateFlowStep.CursorRelocationType = FlowStep.CursorRelocationType;

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

        private List<FlowStep> GetParents(int? flowStepId)
        {

            List<FlowStep> parents = new List<FlowStep>();
            FlowStep? parent = _dataService.FlowSteps.ClearQuery()
                .Include(x => x.ParentFlowStep)
                .FirstOrDefault(x => x.Id == flowStepId.Value);

            while (parent != null)
            {
                switch (parent.Type)
                {
                    case FlowStepTypesEnum.TEMPLATE_SEARCH:
                    case FlowStepTypesEnum.MULTIPLE_TEMPLATE_SEARCH:
                    case FlowStepTypesEnum.WAIT_FOR_TEMPLATE:
                        parents.Add(parent);
                        break;
                    case FlowStepTypesEnum.FAILURE: // Skip Parent if flowStep is of type: Fail.
                        parent = parent.ParentFlowStep;
                        break;
                }

                //Get parent flowStep
                if (parent?.ParentFlowStepId != null)
                    parent = _dataService.FlowSteps.ClearQuery()
                        .Include(x => x.ParentFlowStep)
                        .FirstOrDefault(x => x.Id == parent.ParentFlowStepId);

                //Get parent SubflowStep
                else if (parent?.FlowId != null)
                    parent = _dataService.Flows.ClearQuery()
                        .Where(x => x.Id == parent.FlowId)
                        .Select<FlowStep?>(x => x.ParentSubFlowStep)
                        .FirstOrDefault();
                else
                    parent = null;
            }

            return parents;
        }
    }
}
