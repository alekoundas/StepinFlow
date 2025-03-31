using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Model.Models;
using Business.Helpers;
using Model.Business;
using Model.Enums;
using System.Collections.ObjectModel;
using System.IO;
using Business.BaseViewModels;
using System.Windows.Input;
using StepinFlow.Interfaces;
using Business.Services.Interfaces;
using Business.Factories.FormValidationFactory;

namespace StepinFlow.ViewModels.Pages
{
    public partial class WaitForTemplateFlowStepVM : BaseFlowStepDetailVM
    {
        private readonly ISystemService _systemService;
        private readonly ITemplateSearchService _templateMatchingService;
        private readonly IWindowService _windowService;
        private readonly IDataService _dataService;
        private readonly IFormValidationFactory _formValidationFactory;

        [ObservableProperty]
        private byte[]? _testResultImage = null;

        [ObservableProperty]
        private IEnumerable<TemplateMatchModesEnum> _matchModes;

        [ObservableProperty]
        private ObservableCollection<FlowParameter> _flowParameters = new ObservableCollection<FlowParameter>();

        public WaitForTemplateFlowStepVM(
            ISystemService systemService, 
            ITemplateSearchService templateMatchingService, 
            IDataService dataService,
            IWindowService windowService,
            IFormValidationFactory formValidationFactory) : base(dataService)

        {

            _dataService = dataService;
            _systemService = systemService;
            _templateMatchingService = templateMatchingService;
            _windowService = windowService;
            _formValidationFactory = formValidationFactory;

            MatchModes = Enum.GetValues(typeof(TemplateMatchModesEnum)).Cast<TemplateMatchModesEnum>();
        }

        public override async Task LoadFlowStepId(int flowStepId)
        {
            ValidationHelper.ErrorsChanged += OnErrorsChange;
            TestResultImage = null;
            FlowStep? flowStep = await _dataService.FlowSteps
                .Include(x => x.FlowParameter)
                .FirstOrDefaultAsync(x => x.Id == flowStepId);

            if (flowStep != null)
                FlowStep = flowStep;

            List<FlowParameter> flowParameters = await _dataService.FlowParameters.FindParametersFromFlowStep(flowStepId);
            flowParameters = flowParameters.Where(x => x.Type == FlowParameterTypesEnum.TEMPLATE_SEARCH_AREA).ToList();
            FlowParameters = new ObservableCollection<FlowParameter>(flowParameters);

        }

        public override async Task LoadNewFlowStep(FlowStep newFlowStep)
        {
            ValidationHelper.ErrorsChanged += OnErrorsChange;
            TestResultImage = null;
            FlowStep = newFlowStep;

            List<FlowParameter> flowParameters = await _dataService.FlowParameters.FindParametersFromFlowStep(newFlowStep.ParentFlowStepId.Value);
            flowParameters = flowParameters.Where(x => x.Type == FlowParameterTypesEnum.TEMPLATE_SEARCH_AREA).ToList();
            FlowParameters = new ObservableCollection<FlowParameter>(flowParameters);
            FlowStep.Name = "Wait for Template Search.";

            return;
        }


        [RelayCommand]
        private void OnButtonOpenFileClick()
        {
            Microsoft.Win32.OpenFileDialog openFileDialog = new Microsoft.Win32.OpenFileDialog();
            openFileDialog.InitialDirectory = PathHelper.GetAppDataPath();
            openFileDialog.Filter = "Image files (*.png)|*.png|All Files (*.*)|*.*";
            openFileDialog.RestoreDirectory = true;

            if (openFileDialog.ShowDialog() == true)
            {
                FlowStep.TemplateImage = File.ReadAllBytes(openFileDialog.FileName);
            }

        }

        [RelayCommand]
        private async Task OnButtonTakeScreenshotClick()
        {
            byte[]? resultTemplate = await _windowService.OpenScreenshotSelectionWindow();
            if (resultTemplate == null)
                return;

            FlowStep.TemplateImage = resultTemplate;
        }

        [RelayCommand]
        private async Task OnTemplateImageDoubleClick(MouseButtonEventArgs e)
        {
            // Check if it's a double-click.
            if (e.ClickCount == 2)
            {
                byte[]? image = await _windowService.OpenScreenshotSelectionWindow(FlowStep.TemplateImage);
                if (image != null)
                    FlowStep.TemplateImage = image;
            }
        }

        [RelayCommand]
        private async Task OnResultImageDoubleClick(MouseButtonEventArgs e)
        {
            // Check if it's a double-click.
            if (e.ClickCount == 2)
                await _windowService.OpenScreenshotSelectionWindow(TestResultImage, false);
        }


        [RelayCommand]
        private void OnButtonTestClick()
        {
            // Find search area.
            Model.Structs.Rectangle? searchRectangle = null;
            switch (FlowStep.FlowParameter?.TemplateSearchAreaType)
            {
                case TemplateSearchAreaTypesEnum.SELECT_EVERY_MONITOR:
                    searchRectangle = _systemService.GetScreenSize();
                    break;
                case TemplateSearchAreaTypesEnum.SELECT_MONITOR:
                    searchRectangle = _systemService.GetMonitorArea(FlowStep.FlowParameter.SystemMonitorDeviceName);
                    break;
                case TemplateSearchAreaTypesEnum.SELECT_APPLICATION_WINDOW:
                    searchRectangle = _systemService.GetWindowSize(FlowStep.FlowParameter.ProcessName);
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
            byte[]? screenshot = _systemService.TakeScreenShot(searchRectangle.Value);
            if (screenshot == null)
                return;

            TemplateMatchingResult result = _templateMatchingService.SearchForTemplate(FlowStep.TemplateImage, screenshot, FlowStep.TemplateMatchMode, FlowStep.RemoveTemplateFromResult);
            TestResultImage = result.ResultImage;
        }

        public override async Task<int> OnSave()
        {
            ValidationHelper.ClearErrors();
            _formValidationFactory.CreateValidator("FlowStep.Name").Validate(FlowStep.Name);
            _formValidationFactory.CreateValidator("FlowStep.Accuracy").Validate(FlowStep.Accuracy);
            _formValidationFactory.CreateValidator("FlowStep.TemplateImage").Validate(FlowStep.TemplateImage);
            _formValidationFactory.CreateValidator("FlowStep.FlowParameter").Validate(FlowStep.FlowParameter);
            _formValidationFactory.CreateValidator("FlowStep.TemplateMatchMode").Validate(FlowStep.TemplateMatchMode);

            if (ValidationHelper.HasErrors())
                return -1;



            // Edit mode
            if (FlowStep.Id > 0)
            {
                FlowStep updateFlowStep = await _dataService.FlowSteps.FirstAsync(x => x.Id == FlowStep.Id);
                updateFlowStep.Accuracy = FlowStep.Accuracy;
                updateFlowStep.Name = FlowStep.Name;
                updateFlowStep.ProcessName = FlowStep.ProcessName;
                updateFlowStep.WaitForHours = FlowStep.WaitForHours;
                updateFlowStep.WaitForMinutes = FlowStep.WaitForMinutes;
                updateFlowStep.WaitForSeconds = FlowStep.WaitForSeconds;
                updateFlowStep.WaitForMilliseconds = FlowStep.WaitForMilliseconds;
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



                // "Add" Flow steps
                FlowStep newFlowStep = new FlowStep();
                newFlowStep.Type = FlowStepTypesEnum.NEW;

                // "Success" Flow step
                FlowStep successFlowStep = new FlowStep();
                successFlowStep.Name = "Success";
                successFlowStep.IsExpanded = false;
                successFlowStep.Type = FlowStepTypesEnum.SUCCESS;
                successFlowStep.ChildrenFlowSteps = new ObservableCollection<FlowStep>
                {
                    newFlowStep
                };

                FlowStep.ChildrenFlowSteps = new ObservableCollection<FlowStep>
                {
                    successFlowStep,
                };


                await _dataService.FlowSteps.AddAsync(FlowStep);

            }
            return FlowStep.Id;
        }
    }
}

