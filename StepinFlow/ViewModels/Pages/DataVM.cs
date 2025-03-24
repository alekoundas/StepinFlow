using Business.Helpers;
using Business.Services.Interfaces;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using Model.Models;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows.Forms;
using Wpf.Ui.Abstractions.Controls;

namespace StepinFlow.ViewModels.Pages
{
    public partial class DataVM : ObservableObject, INavigationAware
    {
        private readonly IDataService _dataService;
        private readonly ISystemService _systemService;
        private readonly ICloneService _cloneService;

        [ObservableProperty]
        private string _exportPath = PathHelper.GetExportDataPath();

        [ObservableProperty]
        private string _importFileLocation = string.Empty;

        [ObservableProperty]
        private string _exportedFileLocation = string.Empty;


        // Combobox Flows
        [ObservableProperty]
        private Flow? _comboBoxSelectedFlow = null;

        [ObservableProperty]
        private ObservableCollection<Flow> _comboBoxFlows = new ObservableCollection<Flow>();

        public DataVM(IDataService dataService, ISystemService systemService, ICloneService cloneService)
        {
            _dataService = dataService;
            _systemService = systemService;
            _cloneService = cloneService;

            ComboBoxFlows = new ObservableCollection<Flow>(_dataService.Flows.ToList());
        }

        [RelayCommand]
        private void OnButtonChangeDirectoryClick()
        {
            using (var dialog = new FolderBrowserDialog())
            {
                dialog.Description = "Select a folder";
                dialog.ShowNewFolderButton = true; // Allows user to create a new folder

                DialogResult result = dialog.ShowDialog();

                if (result == System.Windows.Forms.DialogResult.OK)
                {
                    ExportPath = dialog.SelectedPath;
                }
            }
        }

        [RelayCommand]
        private void OnButtonChangeImportFilePathClick()
        {
            Microsoft.Win32.OpenFileDialog openFileDialog = new Microsoft.Win32.OpenFileDialog();
            openFileDialog.InitialDirectory = PathHelper.GetAppDataPath();
            openFileDialog.Filter = "JSON Files (*.json)|*.json|All Files (*.*)|*.*";
            openFileDialog.RestoreDirectory = true;

            if (openFileDialog.ShowDialog() == true)
                ImportFileLocation = openFileDialog.FileName;
        }

        [RelayCommand]
        private void OnButtonResetDirectoryClick()
        {
            ExportPath = PathHelper.GetExportDataPath();
        }

        [RelayCommand]
        private async Task OnButtonExportClick()
        {
            List<Flow> flows = await _dataService.Flows.LoadAllExport(ComboBoxSelectedFlow?.Id);

            string fileDate = DateTime.Now.ToString("yy-MM-dd hh.mm.ss");
            string fileName = "Export " + fileDate + ".json";
            string filePath = Path.Combine(ExportPath, fileName);

            await _systemService.ExportFlowsJSON(flows, filePath);
            ExportedFileLocation = "Exported file: " + filePath;
        }

        [RelayCommand]
        private async Task OnButtonImportClick()
        {
            List<Flow>? flows = _systemService.ImportFlowsJSON(ImportFileLocation);
            List<Flow> clonedFlows = new List<Flow>();

            if (flows != null)
                foreach (Flow flow in flows)
                    clonedFlows.Add(_cloneService.GetFlowClone(flow));

            foreach (Flow clonedFlow in clonedFlows)
                await _dataService.Flows.AddAsync(clonedFlow);


            foreach (Flow clonedFlow in clonedFlows)
                await _dataService.Flows.FixOneToOneRelationIds(clonedFlow.Id);
        }

        [RelayCommand]
        private async Task OnButtonDeleteClick()
        {
            //var executions = _dataService.Executions.ToList();
            //await _dataService.Executions.RemoveRangeAsync(executions);
            await _dataService.CreateNewDbContext.Database.ExecuteSqlRawAsync("DELETE FROM Executions;");
            // Reclaim free space in database file.
            await _dataService.CreateNewDbContext.Database.ExecuteSqlRawAsync("VACUUM;");
        }


        private Flow? FlowStepClone(Flow flow)
        {
            Queue<(FlowStep, FlowStep)> queue = new Queue<(FlowStep, FlowStep)>();
            Dictionary<int, FlowStep> clonedFlowSteps = new Dictionary<int, FlowStep>();

            // Clone the root (Flow).
            Flow clonedFlow = new Flow
            {
                Name = flow.Name,
                IsExpanded = flow.IsExpanded,
                OrderingNum = flow.OrderingNum,
            };

            foreach (FlowStep flowStep in flow.FlowStep.ChildrenFlowSteps)
            {
                FlowStep clonedFlowStep = CreateFlowStepClone(flowStep);
                queue.Enqueue((flowStep, clonedFlowStep));
                clonedFlowSteps.Add(flowStep.Id, clonedFlowStep);
                clonedFlow.FlowStep.ChildrenFlowSteps.Add(clonedFlowStep);
            }

            while (queue.Count > 0)
            {
                var (originalNode, clonedNode) = queue.Dequeue();


                foreach (FlowStep child in originalNode.ChildrenFlowSteps)
                {
                    FlowStep? parentTemplateSearchFlowStep = null;
                    if (child.ParentTemplateSearchFlowStepId.HasValue)
                        parentTemplateSearchFlowStep = clonedFlowSteps
                            .Where(x => x.Key == child.ParentTemplateSearchFlowStepId.Value)
                            .FirstOrDefault()
                            .Value;

                    var clonedChild = CreateFlowStepClone(child, clonedNode, parentTemplateSearchFlowStep);

                    // Add to the parent's children
                    clonedNode.ChildrenFlowSteps.Add(clonedChild);

                    // Enqueue for further processing
                    queue.Enqueue((child, clonedChild));
                    clonedFlowSteps.Add(child.Id, clonedChild);

                }

                // Template search flow steps.
                foreach (FlowStep child in originalNode.ChildrenTemplateSearchFlowSteps)
                    clonedNode.ChildrenTemplateSearchFlowSteps.Add(CreateFlowStepClone(child));
            }

            return clonedFlow;

        }

        private FlowStep CreateFlowStepClone(FlowStep flowStep, FlowStep? parentFlowStep = null, FlowStep? parentTemplateSearchFlowStep = null)
        {
            return new FlowStep
            {
                ParentFlowStep = parentFlowStep,
                ParentTemplateSearchFlowStep = parentTemplateSearchFlowStep,
                Name = flowStep.Name,
                ProcessName = flowStep.ProcessName,
                IsExpanded = flowStep.IsExpanded,
                IsSelected = false,
                OrderingNum = flowStep.OrderingNum,
                Type = flowStep.Type,
                TemplateImage = flowStep.TemplateImage,
                Accuracy = flowStep.Accuracy,
                LocationX = flowStep.LocationX,
                LocationY = flowStep.LocationY,
                LoopCount = flowStep.LoopCount,
                RemoveTemplateFromResult = flowStep.RemoveTemplateFromResult,
                CursorAction = flowStep.CursorAction,
                CursorButton = flowStep.CursorButton,
                CursorScrollDirection = flowStep.CursorScrollDirection,
                IsLoopInfinite = flowStep.IsLoopInfinite,
                LoopTime = flowStep.LoopTime,
                WaitForHours = flowStep.WaitForHours,
                WaitForMinutes = flowStep.WaitForMinutes,
                WaitForSeconds = flowStep.WaitForSeconds,
                WaitForMilliseconds = flowStep.WaitForMilliseconds,
                Height = flowStep.Height,
                Width = flowStep.Width,
            };
        }
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
