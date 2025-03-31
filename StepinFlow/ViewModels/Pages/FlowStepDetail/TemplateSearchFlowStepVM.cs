using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Model.Models;
using Business.Helpers;
using Model.Business;
using Model.Enums;
using System.Collections.ObjectModel;
using System.IO;
using StepinFlow.Interfaces;
using System.Windows.Input;
using Business.BaseViewModels;
using Business.Services.Interfaces;
using Business.Factories.FormValidationFactory;

namespace StepinFlow.ViewModels.Pages
{
    public partial class TemplateSearchFlowStepVM : BaseFlowStepDetailVM
    {
        private readonly ISystemService _systemService;
        private readonly ITemplateSearchService _templateMatchingService;
        private readonly IDataService _dataService;
        private readonly IWindowService _windowService;
        private readonly IFormValidationFactory _formValidationFactory;


        [ObservableProperty]
        private byte[]? _testResultImage = null;

        [ObservableProperty]
        private IEnumerable<TemplateMatchModesEnum> _matchModes;
        [ObservableProperty]
        private ObservableCollection<FlowParameter> _flowParameters = new ObservableCollection<FlowParameter>();

        public TemplateSearchFlowStepVM(
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
            //TODO: SOS
            // Existing logic to load FlowStep
            //await base.LoadFlowStepId(flowStepId);

            TestResultImage = null;

            // Load lookup.
            List<FlowParameter> flowParameters = await _dataService.FlowParameters.FindParametersFromFlowStep(flowStepId);
            flowParameters = flowParameters.Where(x => x.Type == FlowParameterTypesEnum.TEMPLATE_SEARCH_AREA).ToList();
            FlowParameters = new ObservableCollection<FlowParameter>(flowParameters);

            // Load form entity.
            FlowStep? flowStep = await _dataService.FlowSteps
                .Include(x => x.FlowParameter)
                .FirstOrDefaultAsync(x => x.Id == flowStepId);

            if (flowStep != null)
                FlowStep = flowStep;

        }

        public override async Task LoadNewFlowStep(FlowStep newFlowStep)
        {
            TestResultImage = null;
            FlowStep = newFlowStep;

            List<FlowParameter> flowParameters = await _dataService.FlowParameters.FindParametersFromFlowStep(newFlowStep.ParentFlowStepId.Value);
            flowParameters = flowParameters.Where(x => x.Type == FlowParameterTypesEnum.TEMPLATE_SEARCH_AREA).ToList();
            FlowParameters = new ObservableCollection<FlowParameter>(flowParameters);
            FlowStep.Name = "Template search.";

            return;
        }

        [RelayCommand]
        private void OnButtonOpenFileClick()
        {
            Microsoft.Win32.OpenFileDialog openFileDialog = new Microsoft.Win32.OpenFileDialog
            {
                InitialDirectory = PathHelper.GetAppDataPath(),
                Filter = "Image files (*.png)|*.png|All Files (*.*)|*.*",
                RestoreDirectory = true
            };

            if (openFileDialog.ShowDialog() == true)
                FlowStep.TemplateImage = File.ReadAllBytes(openFileDialog.FileName);
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
        private void OnButtonTestClick()
        {
            if (FlowStep.TemplateImage == null)
                return;

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
            // New if not previous exists.
            // Get previous one if exists.
            byte[]? screenshot;
            if (TestResultImage != null && FlowStep.RemoveTemplateFromResult)
                screenshot = TestResultImage;
            else
                screenshot = _systemService.TakeScreenShot(searchRectangle.Value);

            if (screenshot == null)
                return;

            TemplateMatchingResult result = _templateMatchingService.SearchForTemplate(FlowStep.TemplateImage, screenshot, FlowStep.TemplateMatchMode, FlowStep.RemoveTemplateFromResult);
            TestResultImage = result.ResultImage;
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
        public override void OnPageExit()
        {
            base.OnPageExit();

            //SelectedFlowParameter = null;
            TestResultImage = null;
        }
        public override async Task<int> OnSave()
        {
            Dictionary<string, List<string>> errors = new Dictionary<string, List<string>>();

            errors["FlowStep.Name"] = new List<string>(_formValidationFactory.CreateValidator("FlowStep.Name").Validate(FlowStep.Name));
            errors["FlowStep.Accuracy"] = new List<string>(_formValidationFactory.CreateValidator("FlowStep.Accuracy").Validate(FlowStep.Accuracy));
            errors["FlowStep.TemplateImage"] = new List<string>(_formValidationFactory.CreateValidator("FlowStep.TemplateImage").Validate(FlowStep.TemplateImage));
            errors["FlowStep.FlowParameter"] = new List<string>(_formValidationFactory.CreateValidator("FlowStep.FlowParameter").Validate(FlowStep.FlowParameter));
            errors["FlowStep.TemplateMatchMode"] = new List<string>(_formValidationFactory.CreateValidator("FlowStep.TemplateMatchMode").Validate(FlowStep.TemplateMatchMode));

            ValidationHelper.ClearErrors();
            foreach (var error in errors)
                foreach (var errorMessage in error.Value)
                    ValidationHelper.AddError(error.Key, errorMessage);


            if (ValidationHelper.HasErrors())
            {
                //OnPropertyChanged("FlowStep");
                //OnPropertyChanged("FlowStep.Name");
                //OnPropertyChanged("FlowStep.Accuracy");
                //OnPropertyChanged("FlowStep.TemplateImage");
                //OnPropertyChanged("FlowStep.FlowParameter");
                //OnPropertyChanged("FlowStep.TemplateMatchMode");
                return -1;
            }

            // Edit mode.
            if (FlowStep.Id > 0)
            {
                FlowStep updateFlowStep = await _dataService.FlowSteps.FirstAsync(x => x.Id == FlowStep.Id);
                updateFlowStep.Name = FlowStep.Name;
                updateFlowStep.TemplateMatchMode = FlowStep.TemplateMatchMode;
                updateFlowStep.TemplateImage = FlowStep.TemplateImage;
                updateFlowStep.Accuracy = FlowStep.Accuracy;
                updateFlowStep.IsLoop = FlowStep.IsLoop;
                updateFlowStep.RemoveTemplateFromResult = FlowStep.RemoveTemplateFromResult;
                updateFlowStep.LoopMaxCount = FlowStep.LoopMaxCount;
                updateFlowStep.LoopMaxCount = FlowStep.LoopMaxCount;

                await _dataService.UpdateAsync(updateFlowStep);
            }

            // Add mode.
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

                FlowStep.IsExpanded = true;
                await _dataService.FlowSteps.AddAsync(FlowStep);

            }
            return FlowStep.Id;
        }


    }
}

