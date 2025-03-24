using Model.Models;
using System.Windows;
using CommunityToolkit.Mvvm.Input;
using Model.Enums;
using Microsoft.EntityFrameworkCore;
using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq.Expressions;
using Business.Services.Interfaces;
using Business.Extensions;

namespace StepinFlow.ViewModels.UserControls
{
    public partial class TreeViewUserControlVM : ObservableObject, INotifyPropertyChanged
    {
        private readonly IDataService _dataService;
        private readonly ISystemService _systemService;
        private readonly ICloneService _cloneService;



        public event OnSelectedFlowIdChanged? OnSelectedFlowIdChangedEvent;
        public delegate void OnSelectedFlowIdChanged(int Id);

        public event OnSelectedFlowParameterIdChanged? OnSelectedFlowParameterIdChangedEvent;
        public delegate void OnSelectedFlowParameterIdChanged(int Id);

        public event OnSelectedFlowStepIdChanged? OnSelectedFlowStepIdChangedEvent;
        public delegate void OnSelectedFlowStepIdChanged(int Id);

        public event OnFlowStepClone? OnFlowStepCloneEvent;
        public delegate void OnFlowStepClone(int Id);

        public event OnAddFlowStepClick? OnAddFlowStepClickEvent;
        public delegate void OnAddFlowStepClick(FlowStep newFlowStep);

        public event OnAddFlowParameterClick? OnAddFlowParameterClickEvent;
        public delegate void OnAddFlowParameterClick(FlowParameter newFlowParameter);

        private Flow? _selectedFlow = null;
        private FlowStep? _selectedFlowStep = null;
        private FlowParameter? _selectedFlowParameter = null;


        [ObservableProperty]
        private ObservableCollection<Flow> _flowsList = new ObservableCollection<Flow>();
        [ObservableProperty]
        private bool _isLocked = true;
        [ObservableProperty]
        private int? _coppiedFlowStepId;
        [ObservableProperty]
        private Visibility? _pasteVisibility = Visibility.Collapsed;


        private List<Expression<Func<Flow, bool>>> _loadFilters = new List<Expression<Func<Flow, bool>>>();

        public TreeViewUserControlVM(IDataService dataService, ISystemService systemService, ICloneService cloneService)
        {
            _dataService = dataService;
            _systemService = systemService;
            _cloneService = cloneService;
        }

        public async Task LoadFlows(int flowId = 0, bool isSubFlow = false)
        {
            List<Expression<Func<Flow, bool>>> filters = new List<Expression<Func<Flow, bool>>>();

            if (isSubFlow)
                filters.Add(x => x.Type == FlowTypesEnum.SUB_FLOW);
            else
                filters.Add(x => x.Type == FlowTypesEnum.FLOW);

            if (flowId > 0)
                filters.Add(x => x.Id == flowId);

            var query = _dataService.Flows
                .Include(x => x.FlowStep.ChildrenFlowSteps)
                .Include(x => x.FlowParameter.ChildrenFlowParameters);

            if (_loadFilters.Count > 0)
                foreach (var filter in _loadFilters)
                    query = query.Where(filter);
            else
            {
                _loadFilters = filters;
                foreach (var filter in filters)
                    query = query.Where(filter);
            }


            await Task.Run(async () =>
            {
                List<Flow> flows = await query.ToListAsync();

                // Load children.
                foreach (Flow flow in flows)
                {
                    List<FlowStep> childrenFlowSteps = new List<FlowStep>();
                    foreach (FlowStep flowStep in flow.FlowStep.ChildrenFlowSteps)
                        childrenFlowSteps.Add(await _dataService.FlowSteps.LoadAllExpandedChildren(flowStep));

                    flow.FlowStep.ChildrenFlowSteps = new ObservableCollection<FlowStep>(childrenFlowSteps);
                }

                Application.Current.Dispatcher.Invoke(() =>
                {
                    FlowsList = new ObservableCollection<Flow>(flows);
                });
            });
        }

        public async Task ExpandAndSelectFlow(int id)
        {
            Flow? uiFlow = FlowsList.FirstOrDefault(x => x.Id == id);
            if (uiFlow != null)
            {
                uiFlow.IsSelected = true;
                uiFlow.IsExpanded = true;
            }
        }

        public async Task ExpandAndSelectFlowStep(int id)
        {
            foreach (Flow item in FlowsList)
            {
                List<FlowStep> descendants = item.Descendants().ToList();


                FlowStep? uiFlowStep = descendants.FirstOrDefault(fs => fs.Id == id);
                if (uiFlowStep != null)
                {
                    FlowStep? uiParentFlowStep = descendants.FirstOrDefault(fs => fs.Id == uiFlowStep.ParentFlowStepId);
                    if (uiParentFlowStep != null && uiParentFlowStep?.IsExpanded == false)
                        uiParentFlowStep.IsExpanded = true;

                    uiFlowStep.IsSelected = true;
                    uiFlowStep.IsExpanded = true;
                }
            }
        }

        public async Task ExpandAndSelectFlowParameter(int id)
        {
            foreach (Flow uiFlow in FlowsList)
            {
                FlowParameter? uiFlowParameter = uiFlow.FlowParameter.ChildrenFlowParameters.FirstOrDefault(x => x.Id == id);
                if (uiFlowParameter != null)
                {
                    uiFlowParameter.IsSelected = true;
                    uiFlowParameter.IsExpanded = true;
                }
            }

        }

        public void ClearCopy()
        {
            CoppiedFlowStepId = null;
            PasteVisibility = Visibility.Collapsed;
        }

        public async Task ExpandAll()
        {
            string sqlCommand = "UPDATE FlowSteps SET IsExpanded = 1, IsSelected = 0";
            using (var dbContext = _dataService.CreateNewDbContext)
            {
                int rowsAffected = await dbContext.Database.ExecuteSqlRawAsync(sqlCommand);
            }
            _dataService.Dispose();

            await LoadFlows();
        }

        public async Task CollapseAll()
        {
            string sqlCommand = "UPDATE FlowSteps SET IsExpanded = 0, IsSelected = 0";
            using (var dbContext = _dataService.CreateNewDbContext)
            {
                int rowsAffected = await dbContext.Database.ExecuteSqlRawAsync(sqlCommand);
            }
            _dataService.Dispose();
            await LoadFlows();
        }



        [RelayCommand]
        private void OnButtonCopyClick(FlowStep flowStep)
        {
            CoppiedFlowStepId = flowStep.Id;
            PasteVisibility = Visibility.Visible;

            // Fire event.
            OnFlowStepCloneEvent?.Invoke(flowStep.Id);
        }

        [RelayCommand]
        private void OnButtonNewClick(FlowStep flowStep)
        {
            FlowStep newFlowStep = new FlowStep();

            if (flowStep.ParentFlowStepId.HasValue)
                newFlowStep.ParentFlowStepId = flowStep.ParentFlowStepId;

            if (flowStep.FlowId.HasValue)
                newFlowStep.FlowId = flowStep.FlowId;

            OnAddFlowStepClickEvent?.Invoke(newFlowStep);
        }
        [RelayCommand]
        private void OnButtonNewParameterClick(FlowParameter flowParameter)
        {
            FlowParameter newFlowParameter = new FlowParameter();

            newFlowParameter.ParentFlowParameterId = flowParameter.ParentFlowParameterId;


            OnAddFlowParameterClickEvent?.Invoke(newFlowParameter);
        }

        [RelayCommand]
        private async Task OnButtonPasteClick(FlowStep flowStep)
        {
            FlowStep? clonedFlowStep = null;

            if (CoppiedFlowStepId.HasValue)
                clonedFlowStep = await _cloneService.GetFlowStepClone(CoppiedFlowStepId.Value);

            if (flowStep.ParentFlowStepId.HasValue && clonedFlowStep != null)
            {
                FlowStep isNewSimpling = await _dataService.FlowSteps.GetIsNewSibling(flowStep.ParentFlowStepId.Value);
                clonedFlowStep.OrderingNum = isNewSimpling.OrderingNum;
                isNewSimpling.OrderingNum++;
                await _dataService.UpdateAsync(isNewSimpling);

                clonedFlowStep.ParentFlowStepId = flowStep.ParentFlowStepId;
                await _dataService.FlowSteps.AddAsync(clonedFlowStep);
            }
            else if (flowStep.FlowId.HasValue && clonedFlowStep != null)
            {
                FlowStep isNewSimpling = await _dataService.Flows.GetIsNewSibling(flowStep.FlowId.Value);
                clonedFlowStep.OrderingNum = isNewSimpling.OrderingNum;
                isNewSimpling.OrderingNum++;
                await _dataService.UpdateAsync(isNewSimpling);

                clonedFlowStep.ParentFlowStepId = flowStep.ParentFlowStepId;
                await _dataService.FlowSteps.AddAsync(clonedFlowStep);
            }

            await LoadFlows();
        }



        [RelayCommand]
        private async Task OnFlowStepButtonDeleteClick(FlowStep flowStep)
        {
            FlowStep? deleteFlowStep = await _dataService.FlowSteps.FirstOrDefaultAsync(x => x.Id == flowStep.Id);
            if (deleteFlowStep != null)
                await _dataService.FlowSteps.RemoveAsync(deleteFlowStep);

            await LoadFlows();
        }

        [RelayCommand]
        private async Task OnFlowParameterButtonDeleteClick(FlowParameter flowParameter)
        {
            FlowParameter? deleteFlowParameter = await _dataService.FlowParameters.FirstOrDefaultAsync(x => x.Id == flowParameter.Id);
            if (deleteFlowParameter != null)
                await _dataService.FlowParameters.RemoveAsync(deleteFlowParameter);

            await LoadFlows();
        }


        [RelayCommand]
        private async Task OnFlowButtonDeleteClick(Flow flow)
        {
            Flow? deleteFlow = await _dataService.Flows.FirstOrDefaultAsync(x => x.Id == flow.Id);
            if (deleteFlow != null)
                await _dataService.Flows.RemoveAsync(deleteFlow);

            await LoadFlows();
        }


        [RelayCommand]
        private async Task OnButtonUpClick(FlowStep flowStep)
        {
            List<FlowStep> simplings = await _dataService.FlowSteps.GetSiblings(flowStep.Id);
            List<FlowStep> simplingsAbove = simplings
                .Where(x => x.OrderingNum < flowStep.OrderingNum)
                .Where(x => x.Type != FlowStepTypesEnum.NEW)
                .ToList();

            if (simplingsAbove.Any())
            {
                // Find max
                FlowStep simplingAbove = simplingsAbove.Aggregate((currentMax, x) => x.OrderingNum > currentMax.OrderingNum ? x : currentMax);

                // Swap values
                (flowStep.OrderingNum, simplingAbove.OrderingNum) = (simplingAbove.OrderingNum, flowStep.OrderingNum);

                await _dataService.UpdateAsync(flowStep);
                await _dataService.UpdateAsync(simplingAbove);
                await LoadFlows();
            }
        }

        [RelayCommand]
        private async Task OnButtonDownClick(FlowStep flowStep)
        {
            List<FlowStep> simplings = await _dataService.FlowSteps.GetSiblings(flowStep.Id);
            List<FlowStep> simplingsBellow = simplings
                .Where(x => x.OrderingNum > flowStep.OrderingNum)
                .Where(x => x.Type != FlowStepTypesEnum.NEW)
                .ToList();

            if (simplingsBellow.Any())
            {
                // Find min
                FlowStep simplingBellow = simplingsBellow.Aggregate((currentMin, x) => x.OrderingNum < currentMin.OrderingNum ? x : currentMin);

                // Swap values
                (flowStep.OrderingNum, simplingBellow.OrderingNum) = (simplingBellow.OrderingNum, flowStep.OrderingNum);

                await _dataService.UpdateAsync(flowStep);
                await _dataService.UpdateAsync(simplingBellow);
                await LoadFlows();
            }
        }


        [RelayCommand]
        private async Task OnSelected(RoutedPropertyChangedEventArgs<object> routedPropertyChangedEventArgs)
        {
            object selectedItem = routedPropertyChangedEventArgs.NewValue;
            if (selectedItem is FlowStep flowStep)
            {
                if (_selectedFlowStep != null)
                {
                    _selectedFlowStep.IsSelected = false;
                    await _dataService.UpdateAsync(_selectedFlowStep);
                }

                _selectedFlow = null;
                _selectedFlowStep = flowStep;
                _selectedFlowParameter = null;

                _dataService.Update(_selectedFlowStep);
                OnSelectedFlowStepIdChangedEvent?.Invoke(flowStep.Id);
            }
            else if (selectedItem is Flow flow)
            {
                if (_selectedFlow != null)
                {
                    _selectedFlow.IsSelected = false;
                    await _dataService.UpdateAsync(_selectedFlow);
                }

                _selectedFlow = flow;
                _selectedFlowStep = null;
                _selectedFlowParameter = null;

                await _dataService.UpdateAsync(_selectedFlow);
                OnSelectedFlowIdChangedEvent?.Invoke(flow.Id);
            }
            else if (selectedItem is FlowParameter flowParameter)
            {
                if (_selectedFlowParameter != null)
                {
                    await _dataService.UpdateAsync(_selectedFlowParameter);
                }

                _selectedFlow = null;
                _selectedFlowStep = null;
                _selectedFlowParameter = flowParameter;

                await _dataService.UpdateAsync(_selectedFlowParameter);
                OnSelectedFlowParameterIdChangedEvent?.Invoke(flowParameter.Id);
            }
        }


        [RelayCommand]
        private void OnDoubleClick()
        {
            if (_selectedFlowStep != null)
                _selectedFlowStep.IsExpanded = !_selectedFlowStep.IsExpanded;

            else if (_selectedFlow != null)
                _selectedFlow.IsExpanded = !_selectedFlow.IsExpanded;

            else if (_selectedFlowParameter != null)
                _selectedFlowParameter.IsExpanded = !_selectedFlowParameter.IsExpanded;
        }

        [RelayCommand]
        private async Task OnExpanded(object eventParameter)
        {
            if (eventParameter is FlowStep flowStep)
            {
                foreach (Flow item in FlowsList)
                {
                    FlowStep? uiFlowStep = item.Descendants().FirstOrDefault(fs => fs.Id == flowStep.Id);
                    if (uiFlowStep != null)
                    {
                        await _dataService.UpdateAsync(uiFlowStep);

                        if (!uiFlowStep.ChildrenFlowSteps.SelectMany(x => x.ChildrenFlowSteps).Any())
                        {
                            FlowStep loadedFlowStep = await _dataService.FlowSteps.LoadAllExpandedChildren(uiFlowStep);

                            foreach (FlowStep uiChild in uiFlowStep.ChildrenFlowSteps)
                            {
                                List<FlowStep> flowSteps = loadedFlowStep.ChildrenFlowSteps
                                    .First(x => x.Id == uiChild.Id).ChildrenFlowSteps
                                    .ToList();

                                uiChild.ChildrenFlowSteps = new ObservableCollection<FlowStep>(flowSteps);
                            }
                        }
                    }
                }

            }
            else if (eventParameter is Flow flow)
            {
                Flow? updateFlow = FlowsList.FirstOrDefault(x => x.Id == flow.Id);
                if (updateFlow != null)
                {
                    await _dataService.UpdateAsync(updateFlow);
                }
            }
            else if (eventParameter is FlowParameter flowParameter)
            {
                foreach (Flow uiFlow in FlowsList)
                {
                    FlowParameter? uiFlowParameter = uiFlow.FlowParameter.ChildrenFlowParameters.FirstOrDefault(x => x.Id == flowParameter.Id);
                    if (uiFlowParameter != null)
                        await _dataService.UpdateAsync(uiFlowParameter);
                }
            }
        }

        [RelayCommand]
        private async Task OnCollapsed(object eventParameter)
        {
            if (eventParameter is FlowStep flowStep)
            {
                foreach (Flow item in FlowsList)
                {
                    FlowStep? uiFlowStep = item.Descendants().FirstOrDefault(fs => fs.Id == flowStep.Id);
                    if (uiFlowStep != null)
                    {
                        await _dataService.UpdateAsync(uiFlowStep);
                    }
                }
            }
            else if (eventParameter is Flow flow)
            {
                Flow? updateFlow = FlowsList.FirstOrDefault(x => x.Id == flow.Id);
                if (updateFlow != null)
                {
                    await _dataService.UpdateAsync(updateFlow);
                }
            }
            else if (eventParameter is FlowParameter flowParameter)
            {
                foreach (Flow uiFlow in FlowsList)
                {
                    FlowParameter? uiFlowParameter = uiFlow.FlowParameter.ChildrenFlowParameters.FirstOrDefault(x => x.Id == flowParameter.Id);
                    if (uiFlowParameter != null)
                    {
                        await _dataService.UpdateAsync(uiFlowParameter);
                    }
                }
            }
        }
    }
}
