using Business.Extensions;
using Business.Factories;
using Business.Services.Interfaces;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using Model.Enums;
using Model.Models;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using Wpf.Ui.Abstractions.Controls;

namespace StepinFlow.ViewModels.Pages
{
    public partial class ExecutionVM : ObservableObject, INavigationAware
    {
        private readonly IDataService _dataService;
        private readonly ISystemService _systemService;
        private readonly IExecutionFactory _executionFactory;

        public event LoadFlowsEvent? LoadFlows;
        public delegate Task LoadFlowsEvent(int id = 0);

        public event NavigateToExecutionEvent? NavigateToExecution;
        public delegate void NavigateToExecutionEvent(Execution execution);
        public event ExpandAndSelectFlowStepEvent? ExpandAndSelectFlowStep;
        public delegate Task ExpandAndSelectFlowStepEvent(int id);

        // Combobox Flows
        [ObservableProperty]
        private Flow? _comboBoxSelectedFlow;
        [ObservableProperty]
        private ObservableCollection<Flow> _comboBoxFlows = new ObservableCollection<Flow>();

        // Combobox Execution history
        [ObservableProperty]
        private Execution? _comboBoxSelectedExecutionHistory;
        [ObservableProperty]
        private ObservableCollection<Execution> _comboBoxExecutionHistories = new ObservableCollection<Execution>();

        // Listbox executions
        [ObservableProperty]
        private Execution? _listboxSelectedExecution;
        [ObservableProperty]
        public ObservableCollection<Execution> _listBoxExecutions = new ObservableCollection<Execution>();

        [ObservableProperty]
        public string _status = "-";
        [ObservableProperty]
        public string _runFor = "";
        [ObservableProperty]
        public string _currentStep = "";
        [ObservableProperty]
        public bool _isLocked = true;


        private bool _stopExecution = false;
        private CancellationTokenSource _cancellationToken = new CancellationTokenSource();

        private readonly DispatcherTimer _timer;
        private TimeSpan _timeElapsed;


        public ExecutionVM(
            IDataService dataService,
            ISystemService systemService,
            IExecutionFactory executionFactory)
        {
            _dataService = dataService;
            _systemService = systemService;
            _executionFactory = executionFactory;

            List<Flow> flows = _dataService.Flows.Where(x => x.Type == FlowTypesEnum.FLOW).ToList();
            ComboBoxFlows = new ObservableCollection<Flow>(flows);

            _executionFactory.SetCancellationToken(_cancellationToken);

            // Update every second
            _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        }


        [RelayCommand]
        private async Task OnButtonStartClick()
        {
            if (ComboBoxSelectedFlow == null)
                return;

            _timeElapsed = TimeSpan.Zero;
            void UpdateTimer(object sender, EventArgs e)
            {
                _timeElapsed = _timeElapsed.Add(TimeSpan.FromSeconds(1));
                RunFor = _timeElapsed.ToString(@"hh\:mm\:ss");
            }
            _timer.Tick += UpdateTimer;
            _timer.Start();

            Status = ExecutionStatusEnum.RUNNING.ToString();

            // Create new thread so UI doesnt freeze.
            await Task.Run(async () =>
                {
                    IExecutionWorker flowWorker = _executionFactory.GetWorker(null);
                    Execution flowExecution = await flowWorker.CreateExecutionModelFlow(ComboBoxSelectedFlow.Id, null);
                    FlowStep? nextFlowStep;

                    // Add Execution to listbox and select it
                    List<Execution> executions = ComboBoxExecutionHistories.ToList();
                    executions.Add(flowExecution);
                    ComboBoxExecutionHistories = new ObservableCollection<Execution>(executions);
                    ComboBoxSelectedExecutionHistory = flowExecution;

                    await flowWorker.SetExecutionModelStateRunning(flowExecution);
                    await flowWorker.SaveToDisk(flowExecution);

                    nextFlowStep = await flowWorker.GetNextChildFlowStep(flowExecution);
                    await ExecuteStepLoop(nextFlowStep, flowExecution);
                    //TODO: Enable this
                    //await flowWorker.SetExecutionModelStateComplete(flowExecution);
                    _timer.Stop();
                    Status = ExecutionStatusEnum.COMPLETED.ToString();

                });

            _stopExecution = false;
            _cancellationToken = new CancellationTokenSource();
            _executionFactory.SetCancellationToken(_cancellationToken);
            _executionFactory.DestroyWorkers();

        }
        private long GetObjectSize(object obj)
        {
            using (var stream = new MemoryStream())
            {
                var serializer = new DataContractSerializer(obj.GetType());
                serializer.WriteObject(stream, obj);
                return stream.Length;
            }
        }

        private async Task ExecuteStepLoop(FlowStep? initialFlowStep, Execution parentExecution)
        {

            int counter = 0;
            Dictionary<int, FlowStep> flowStepCache = new Dictionary<int, FlowStep>();


            var pendingExecutionLoops = new Dictionary<int, List<Execution>>();
            var stack = new Stack<FlowStep?>();

            stack.Push(initialFlowStep);
            using (var context = _dataService.CreateNewDbContext) // Replace with your DbContext
            {

                while (stack.Count > 0)
                {
                using var tranasaction = context.Database.BeginTransaction();
                    counter++;
                    if (counter == 1000)
                    {

                    }
                    FlowStep? flowStep = stack.Pop();

                    if (flowStep == null || _stopExecution == true)
                        return;

                    // Create factory worker.
                    IExecutionWorker factoryWorker = _executionFactory.GetWorker(flowStep.Type);
                    factoryWorker.SetDbContext(context);
                    factoryWorker.Initialize(pendingExecutionLoops);

                    //factoryWorker.ClearEntityFrameworkChangeTracker();



                    // Create execution model.
                    Execution flowStepExecution = await factoryWorker.CreateExecutionModel(flowStep, parentExecution);

                    //var size = GetObjectSize(flowStepExecution); // See helper method below
                    //Console.WriteLine($"Execution {flowStepExecution.Id} size: {size} bytes");

                    // Add execution to history listbox.
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        CurrentStep = flowStep.Type.ToString();
                        if (flowStepExecution?.FlowStep?.TemplateImage?.Length == 0)
                            flowStepExecution.FlowStep.TemplateImage = null;

                        if (flowStepExecution?.FlowStepId != null)
                        {
                            if (flowStepCache.ContainsKey(flowStepExecution.FlowStepId.Value))
                                flowStepExecution.FlowStep = flowStepCache[flowStepExecution.FlowStepId.Value];
                            else
                                flowStepCache[flowStepExecution.FlowStepId.Value] = flowStepExecution.FlowStep;
                        }

                        ListBoxExecutions.Add(flowStepExecution);
                    });


                    //await ExpandAndSelectFlowStep?.Invoke(flowStepExecution.FlowStepId ?? -1);
                    //await factoryWorker.ExpandAndSelectFlowStep(flowStepExecution, _treeViewUserControlViewModel.FlowsList);
                    //context.ChangeTracker.Clear();
                    await factoryWorker.SetExecutionModelStateRunning(flowStepExecution);
                    await factoryWorker.ExecuteFlowStepAction(flowStepExecution);
                    await factoryWorker.SetExecutionModelStateComplete(flowStepExecution);
                    await factoryWorker.SaveToDisk(flowStepExecution);

                    // If step has a sibling, push it first in stack.
                    FlowStep? nextFlowStep;
                    nextFlowStep = await factoryWorker.GetNextSiblingFlowStep(flowStepExecution);
                    if (nextFlowStep != null)
                        stack.Push(nextFlowStep);

                    // If child is found, push it to stack last so it can be executed firtst.
                    nextFlowStep = await factoryWorker.GetNextChildFlowStep(flowStepExecution);
                    if (nextFlowStep != null)
                        stack.Push(nextFlowStep);

                    parentExecution = flowStepExecution;
                    await tranasaction.CommitAsync();

                    //size = GetObjectSize(flowStepExecution); // See helper method below
                    //Console.WriteLine($"Execution {flowStepExecution.Id} size: {size} bytes");
                tranasaction.Dispose();
                }
                _dataService.Dispose(true);
            }
        }

        [RelayCommand]
        private void OnButtonStopClick()
        {
            _stopExecution = true;
            _cancellationToken.Cancel();
        }


        [RelayCommand]
        private async Task OnComboBoxSelectionChangedFlow(SelectionChangedEventArgs routedPropertyChangedEventArgs)
        {
            if (ComboBoxSelectedFlow == null)
                return;

            List<Execution> executions = await _dataService.Executions
                .Where(x => x.FlowId == ComboBoxSelectedFlow.Id)
                .ToListAsync();

            ComboBoxExecutionHistories = new ObservableCollection<Execution>(executions);
            await LoadFlows?.Invoke(ComboBoxSelectedFlow.Id);
        }


        //private async Task LoadExecutionChild(Execution execution)
        //{
        //    Execution? executionChild = await _dataService.Executions
        //                .Where(x => x.Id == execution.Id)
        //                .Select(x => x.ChildExecution)
        //                .FirstOrDefaultAsync();

        //    if (executionChild == null)
        //        return;

        //    execution.ChildExecution = executionChild;
        //    await LoadExecutionChild(execution.ChildExecution);
        //}


        [RelayCommand]
        private async Task OnButtonDeleteClick()
        {

            //Delete all.
            var aa = _dataService.Executions.ToList();
            await _dataService.Executions.RemoveRangeAsync(aa);

            // Reclaim free space in database file.
            await _dataService.CreateNewDbContext.Database.ExecuteSqlRawAsync("VACUUM;");

            //ComboBoxExecutionHistories.Remove(ComboBoxSelectedExecutionHistory);
            ComboBoxSelectedExecutionHistory = null;
            ListBoxExecutions.Clear();

        }



        [RelayCommand]
        private void OnListBoxSelectedItemChanged(SelectionChangedEventArgs routedPropertyChangedEventArgs)
        {
            if (routedPropertyChangedEventArgs?.AddedItems.Count > 0)
            {
                object? selectedItem = routedPropertyChangedEventArgs.AddedItems[0];
                if (selectedItem is not Execution)
                    return;

                Execution selectedExecution = (Execution)selectedItem;
                ListboxSelectedExecution = selectedExecution;

                if (selectedExecution.Flow != null)
                    selectedExecution.Flow.IsSelected = true;

                if (selectedExecution.FlowStep != null)
                {
                    selectedExecution.FlowStep.IsSelected = true;
                    NavigateToExecution?.Invoke(selectedExecution);
                }
            }
        }

        [RelayCommand]
        private async Task OnComboBoxSelectionChangedExecution(SelectionChangedEventArgs routedPropertyChangedEventArgs)
        {
            if (ComboBoxSelectedFlow == null || ComboBoxSelectedExecutionHistory == null)
            {
                ListBoxExecutions.Clear();
                return;
            }

            //Execution? execution = await _dataService.Executions
            //    .Include(x => x.ChildExecution)
            //    .FirstOrDefaultAsync(x => x.Id == ComboBoxSelectedExecutionHistory.Id);

            ////if (execution != null)
            ////    await LoadExecutionChild(execution);

            //List<Execution> executions = execution
            //    .SelectRecursive(x => x.ChildExecution)
            //    .Where(x => x != null)
            //    .ToList();

            //executions.Add(execution);
            //executions.OrderBy(x => x.Id);

            //ListBoxExecutions = new ObservableCollection<Execution>(executions);
        }
        [RelayCommand]
        private void OnImageFailed(ExceptionRoutedEventArgs e)
        {
            Debug.WriteLine("Image failed to load: " + e.ErrorException?.Message);
        }


        public void OnNavigatedTo() { }

        public void OnNavigatedFrom() { }

        public Task OnNavigatedToAsync()
        {
            return Task.CompletedTask;
        }

        public Task OnNavigatedFromAsync()
        {
            return Task.CompletedTask;
        }
    }
}
