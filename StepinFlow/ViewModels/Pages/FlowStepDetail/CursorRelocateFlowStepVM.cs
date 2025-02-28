﻿using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Model.Models;
using Business.Interfaces;
using DataAccess.Repository.Interface;
using System.Collections.ObjectModel;
using Model.Enums;
using Model.Structs;
using Business.BaseViewModels;
using Microsoft.EntityFrameworkCore;
using Business.Services;
using System.Windows.Input;

namespace StepinFlow.ViewModels.Pages
{
    public partial class CursorRelocateFlowStepVM : BaseFlowStepDetailVM
    {
        private readonly IDataService _dataService;
        private readonly ISystemService _systemService;
        private readonly IKeyboardListenerService _keyboardListenerService;
        public override event Action<int> OnSave;


        [ObservableProperty]
        private ObservableCollection<FlowStep> _parents = new ObservableCollection<FlowStep>();

        [ObservableProperty]
        private FlowStep? _selectedFlowStep = null;
        [ObservableProperty]
        private IEnumerable<CursorRelocationTypesEnum> _cursorRelocationTypesEnum;

        public CursorRelocateFlowStepVM(
            IDataService dataService, 
            ISystemService systemService,
            IKeyboardListenerService keyboardListenerService) : base(dataService)
        {
            _dataService = dataService;
            _systemService = systemService;
            _keyboardListenerService = keyboardListenerService;

            CursorRelocationTypesEnum = Enum.GetValues(typeof(CursorRelocationTypesEnum)).Cast<CursorRelocationTypesEnum>();


            KeyCombination _combination = new KeyCombination(ModifierKeys.None, Key.F3);
            _keyboardListenerService.RegisterListener(_combination, () =>
            {
                OnButtonRecordClick();
            });
        }

        public override async Task LoadFlowStepId(int flowStepId)
        {
            FlowStep? flowStep = await _dataService.FlowSteps.FirstOrDefaultAsync(x => x.Id == flowStepId);
            if (flowStep != null)
            {
                FlowStep = flowStep;
                 if (FlowStep.ParentFlowStepId.HasValue)
                    GetParents(FlowStep.ParentFlowStepId.Value);

                SelectedFlowStep = Parents.FirstOrDefault(x => x.Id == flowStep.ParentTemplateSearchFlowStepId);

                KeyCombination _combination = new KeyCombination(ModifierKeys.None, Key.F3);
                _keyboardListenerService.RegisterListener(_combination, () =>
                {
                    Console.WriteLine("Ctrl + S pressed globally from Form2!");
                });
            }
        }

        public override async Task LoadNewFlowStep(FlowStep newFlowStep)
        {
            FlowStep = newFlowStep;

            if (FlowStep.ParentFlowStepId.HasValue)
                GetParents(FlowStep.ParentFlowStepId.Value);


            KeyCombination _combination = new KeyCombination(ModifierKeys.None, Key.F4);
            _keyboardListenerService.RegisterListener(_combination, () =>
            {
                Console.WriteLine("Ctrl + S pressed globally from Form2!");
            });
            return;
        }

        public override  void OnPageExit()
        {
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
                updateFlowStep.CursorRelocationType = FlowStep.CursorRelocationType;
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

            FlowStep? parent = _dataService.FlowSteps.Query
                .AsNoTracking()
                .Include(x => x.ParentFlowStep)
                .FirstOrDefault(x => x.Id == flowStepId.Value);

            while (parent != null)
            {
                switch (parent.Type)
                {
                    case FlowStepTypesEnum.TEMPLATE_SEARCH:
                    case FlowStepTypesEnum.MULTIPLE_TEMPLATE_SEARCH:
                    case FlowStepTypesEnum.WAIT_FOR_TEMPLATE:
                        Parents.Add(parent);
                        break;
                    case FlowStepTypesEnum.FAILURE: // Skip Parent if flowStep is of type: Fail.
                        parent = parent.ParentFlowStep;
                        break;
                }

                //Get parent flowStep
                if (parent?.ParentFlowStepId != null)
                    parent = _dataService.FlowSteps.Query
                        .AsNoTracking()
                        .Include(x => x.ParentFlowStep)
                        .FirstOrDefault(x => x.Id == parent.ParentFlowStepId);

                //Get parent SubflowStep
                else if (parent?.FlowId != null)
                    parent = _dataService.Flows.Query
                        .AsNoTracking()
                        .Where(x => x.Id == parent.FlowId)
                        .Select(x => x.ParentSubFlowStep)
                        .FirstOrDefault();
                else
                    parent = null;
            }
        }
    }
}
