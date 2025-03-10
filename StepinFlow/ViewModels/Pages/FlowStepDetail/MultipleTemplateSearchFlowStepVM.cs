using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Model.Models;
using Business.Helpers;
using Model.Business;
using Model.Enums;
using System.Collections.ObjectModel;
using System.IO;
using StepinFlow.Interfaces;
using System.Windows.Media.Imaging;
using System.Windows.Input;
using Business.BaseViewModels;
using Business.Services.Interfaces;

namespace StepinFlow.ViewModels.Pages
{
    public partial class MultipleTemplateSearchFlowStepVM : BaseFlowStepDetailVM
    {
        private readonly ISystemService _systemService;
        private readonly ITemplateSearchService _templateMatchingService;
        private readonly IDataService _dataService;
        private readonly IWindowService _windowService;
        //public override event Action<int> OnSave;

        [ObservableProperty]
        private byte[]? _testResultImage = null;
        [ObservableProperty]
        private List<string> _processList = SystemProcessHelper.GetProcessWindowTitles();

        [ObservableProperty]
        private ObservableCollection<FlowStep> _childrenTemplateSearchFlowSteps = new ObservableCollection<FlowStep>();
        private List<FlowStep> _childrenTemplateSearchFlowStepsToRemove = new List<FlowStep>();

        [ObservableProperty]
        private IEnumerable<TemplateMatchModesEnum> _matchModes;
        [ObservableProperty]
        private ObservableCollection<FlowParameter> _flowParameters = new ObservableCollection<FlowParameter>();
        [ObservableProperty]
        private FlowParameter? _selectedFlowParameter = null;

        public MultipleTemplateSearchFlowStepVM(
            ISystemService systemService,
            ITemplateSearchService templateMatchingService,
            IDataService dataService,
            IWindowService windowService) : base(dataService)

        {
            _dataService = dataService;
            _systemService = systemService;
            _templateMatchingService = templateMatchingService;
            _windowService = windowService;

            MatchModes = Enum.GetValues(typeof(TemplateMatchModesEnum)).Cast<TemplateMatchModesEnum>();
        }

        public override async Task LoadFlowStepId(int flowStepId)
        {
            SelectedFlowParameter = null;
            TestResultImage = null;
            FlowStep? flowStep = await _dataService.FlowSteps
                .Include(x => x.ChildrenTemplateSearchFlowSteps)
                .Include(x => x.FlowParameter)
                .FirstOrDefaultAsync(x => x.Id == flowStepId);

            if (flowStep != null)
            {
                FlowStep = flowStep;
                List<FlowStep> flowSteps = flowStep.ChildrenTemplateSearchFlowSteps.Where(x => x.Type == FlowStepTypesEnum.MULTIPLE_TEMPLATE_SEARCH_CHILD).ToList();
                ChildrenTemplateSearchFlowSteps = new ObservableCollection<FlowStep>(flowSteps);


                List<FlowParameter> flowParameters = await _dataService.FlowParameters.FindParametersFromFlowStep(flowStepId);
                flowParameters = flowParameters.Where(x => x.Type == FlowParameterTypesEnum.TEMPLATE_SEARCH_AREA).ToList();
                FlowParameters = new ObservableCollection<FlowParameter>(flowParameters);

                SelectedFlowParameter = FlowParameters.Where(x=>x.Id==FlowStep.FlowParameterId).FirstOrDefault();
            }
        }

        public override async Task LoadNewFlowStep(FlowStep newFlowStep)
        {
            SelectedFlowParameter = null;
            TestResultImage = null;
            FlowStep = newFlowStep;

            List<FlowParameter> flowParameters = await _dataService.FlowParameters.FindParametersFromFlowStep(newFlowStep.ParentFlowStepId.Value);
            flowParameters = flowParameters.Where(x => x.Type == FlowParameterTypesEnum.TEMPLATE_SEARCH_AREA).ToList();
            FlowParameters = new ObservableCollection<FlowParameter>(flowParameters);

            return;
        }

        [RelayCommand]
        private void OnButtonOpenFileClick(FlowStep flowStep)
        {
            Microsoft.Win32.OpenFileDialog openFileDialog = new Microsoft.Win32.OpenFileDialog
            {
                InitialDirectory = PathHelper.GetAppDataPath(),
                Filter = "Image files (*.png)|*.png|All Files (*.*)|*.*",
                RestoreDirectory = true
            };

            if (openFileDialog.ShowDialog() == true)
                flowStep.TemplateImage = File.ReadAllBytes(openFileDialog.FileName);
        }

        [RelayCommand]
        private async Task OnButtonTakeScreenshotClick(FlowStep flowStep)
        {
            byte[]? resultTemplate = await _windowService.OpenScreenshotSelectionWindow();
            if (resultTemplate == null)
                return;

            flowStep.TemplateImage = resultTemplate;
        }


        [RelayCommand]
        private void OnButtonUpClick(FlowStep flowStep)
        {
            List<FlowStep> simplingsAbove = ChildrenTemplateSearchFlowSteps
                    .Where(x => x.OrderingNum < flowStep.OrderingNum)
                    .ToList();

            flowStep.OrderingNum++;
            ChildrenTemplateSearchFlowSteps = new ObservableCollection<FlowStep>(ChildrenTemplateSearchFlowSteps.OrderBy(x => x.OrderingNum).ToList());

        }

        [RelayCommand]
        private void OnButtonDownClick(FlowStep flowStep)
        {
            List<FlowStep> simplingsBellow = ChildrenTemplateSearchFlowSteps
                    .Where(x => x.OrderingNum > flowStep.OrderingNum)
                    .ToList();

            flowStep.OrderingNum--;
            ChildrenTemplateSearchFlowSteps = new ObservableCollection<FlowStep>(ChildrenTemplateSearchFlowSteps.OrderBy(x => x.OrderingNum).ToList());
        }

        [RelayCommand]
        public void OnButtonAddClick()
        {
            FlowStep newFlowStep = new FlowStep();

            // In edit mode set parent ID.
            if (FlowStep.Id > 0)
                newFlowStep.ParentTemplateSearchFlowStepId = FlowStep.Id;

            ChildrenTemplateSearchFlowSteps.Add(newFlowStep);
        }

        [RelayCommand]
        private void OnButtonDeleteClick(FlowStep flowStep)
        {
            if (flowStep.Id > 0)
                _childrenTemplateSearchFlowStepsToRemove.Add(flowStep);

            ChildrenTemplateSearchFlowSteps.Remove(flowStep);
        }

        [RelayCommand]
        private void OnButtonClearTestClick(FlowStep flowStep)
        {
            TestResultImage = null;
        }

        [RelayCommand]
        private void OnButtonTestClick(FlowStep flowStep)
        {
            if (flowStep.TemplateImage == null)
                return;

            // Find search area.
            Model.Structs.Rectangle? searchRectangle = null;
            switch (SelectedFlowParameter?.TemplateSearchAreaType)
            {
                case TemplateSearchAreaTypesEnum.SELECT_EVERY_MONITOR:
                    searchRectangle = _systemService.GetScreenSize();
                    break;
                case TemplateSearchAreaTypesEnum.SELECT_MONITOR:
                    searchRectangle = _systemService.GetMonitorArea(SelectedFlowParameter.SystemMonitorDeviceName);
                    break;
                case TemplateSearchAreaTypesEnum.SELECT_APPLICATION_WINDOW:
                    searchRectangle = _systemService.GetWindowSize(SelectedFlowParameter.ProcessName);
                    break;
                case TemplateSearchAreaTypesEnum.SELECT_CUSTOM_AREA:
                    break;
                default:
                    searchRectangle = _systemService.GetScreenSize();
                    break;
            }

            if (searchRectangle == null)
                searchRectangle = _systemService.GetScreenSize();


            // Get screenshot.
            // New if not previous exists.
            // Get previous one if exists.
            byte[]? screenshot;
            if (TestResultImage != null)
                screenshot = TestResultImage;
            else
                screenshot = _systemService.TakeScreenShot(searchRectangle.Value);

            if (screenshot == null)
                return;


            TemplateMatchingResult result = _templateMatchingService.SearchForTemplate(flowStep.TemplateImage, screenshot, FlowStep.TemplateMatchMode, flowStep.RemoveTemplateFromResult);
            TestResultImage = result.ResultImage;
        }

        [RelayCommand]
        private async Task OnTemplateImageDoubleClick(MouseButtonEventArgs e)
        {

            // Check if it's a double-click.
            if (e.ClickCount == 2)
            {
                if (e.Source is System.Windows.Controls.Image img && img.Source is BitmapSource bitmapSource)
                {
                    using (MemoryStream stream = new MemoryStream())
                    {
                        BitmapEncoder encoder = new PngBitmapEncoder();
                        encoder.Frames.Add(BitmapFrame.Create(bitmapSource));
                        encoder.Save(stream);
                        byte[]? image = await _windowService.OpenScreenshotSelectionWindow(stream.ToArray());
                        if (image != null)
                            using (var stream2 = new MemoryStream(image))
                            {
                                var decoder = BitmapDecoder.Create(stream2, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad);
                                img.Source = decoder.Frames[0];
                            }
                    }
                }

            }
        }

        [RelayCommand]
        private async Task OnResultImageDoubleClick(MouseButtonEventArgs e)
        {
            // Check if it's a double-click.
            if (e.ClickCount == 2)
                await _windowService.OpenScreenshotSelectionWindow(TestResultImage, false);
        }

        public override void OnPageExit()
        {
            ChildrenTemplateSearchFlowSteps = new ObservableCollection<FlowStep>();
            _childrenTemplateSearchFlowStepsToRemove = new List<FlowStep>();
            SelectedFlowParameter = null;

        }

        public override async Task OnSave()
        {
            // Remove flow steps that dont contain a template image.
            List<FlowStep> templateFlowSteps = ChildrenTemplateSearchFlowSteps
                .Where(x => x.TemplateImage == null)
                .ToList();

            foreach (var templateFlowStep in templateFlowSteps)
                ChildrenTemplateSearchFlowSteps.Remove(templateFlowStep);

            foreach (var templateFlowStep in ChildrenTemplateSearchFlowSteps)
                templateFlowStep.Type = FlowStepTypesEnum.MULTIPLE_TEMPLATE_SEARCH_CHILD;

            // Edit mode
            if (FlowStep.Id > 0)
            {
                FlowStep updateFlowStep = await _dataService.FlowSteps.FirstAsync(x => x.Id == FlowStep.Id);
                updateFlowStep.Name = FlowStep.Name;
                updateFlowStep.TemplateMatchMode = FlowStep.TemplateMatchMode;
                if (SelectedFlowParameter != null)
                    updateFlowStep.FlowParameterId = SelectedFlowParameter.Id;

                await _dataService.UpdateRangeAsync(ChildrenTemplateSearchFlowSteps.Where(x => x.Id > 0).ToList());
                await _dataService.FlowSteps.AddRangeAsync(ChildrenTemplateSearchFlowSteps.Where(x => x.Id == 0).ToList());
                await _dataService.FlowSteps.RemoveRangeAsync(_childrenTemplateSearchFlowStepsToRemove);
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



                // "Success" Flow step
                FlowStep successFlowStep = new FlowStep
                {
                    Name = "Success",
                    IsExpanded = false,
                    Type = FlowStepTypesEnum.SUCCESS,
                    ChildrenFlowSteps = new ObservableCollection<FlowStep>
                    {
                        new FlowStep(){Type = FlowStepTypesEnum.NEW}
                    }
                };

                // "Fail" Flow step
                FlowStep failFlowStep = new FlowStep
                {
                    Name = "Fail",
                    IsExpanded = false,
                    Type = FlowStepTypesEnum.FAILURE,
                    ChildrenFlowSteps = new ObservableCollection<FlowStep>
                    {
                        new FlowStep(){Type = FlowStepTypesEnum.NEW}
                    }
                };

                FlowStep.ChildrenFlowSteps = new ObservableCollection<FlowStep>
                {
                    successFlowStep,
                    failFlowStep
                };

                if (FlowStep.Name.Length == 0)
                    FlowStep.Name = "Multiple template search.";

                FlowStep.IsExpanded = true;
                FlowStep.ChildrenTemplateSearchFlowSteps = ChildrenTemplateSearchFlowSteps;
                if (SelectedFlowParameter != null)
                    FlowStep.FlowParameterId = SelectedFlowParameter.Id;

                await _dataService.FlowSteps.AddAsync(FlowStep);

            }



            List<FlowStep> flowSteps = await _dataService.FlowSteps
                .Where(x => x.ParentTemplateSearchFlowStepId == FlowStep.Id)
                .Where(x => x.Type == FlowStepTypesEnum.MULTIPLE_TEMPLATE_SEARCH_CHILD)
                .ToListAsync();
            ChildrenTemplateSearchFlowSteps = new ObservableCollection<FlowStep>(flowSteps);
        }
    }
}

