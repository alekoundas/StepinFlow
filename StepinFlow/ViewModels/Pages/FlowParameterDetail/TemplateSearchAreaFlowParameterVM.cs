using CommunityToolkit.Mvvm.Input;
using Model.Models;
using Business.BaseViewModels;
using CommunityToolkit.Mvvm.ComponentModel;
using Model.Enums;
using Model.Structs;
using Model.Business;
using Business.Helpers;
using Business.Services.Interfaces;

namespace StepinFlow.ViewModels.Pages.FlowParameterDetail
{
    public partial class TemplateSearchAreaFlowParameterVM : BaseFlowParameterDetailVM
    {
        private readonly IDataService _dataService;
        private readonly ISystemService _systemService;
        private readonly FlowsVM _flowsViewModel;

        [ObservableProperty]
        private SystemMonitor _searchAreaRectangle = new SystemMonitor();


        [ObservableProperty]
        private IEnumerable<SystemMonitor> _systemMonitors;
        [ObservableProperty]
        private SystemMonitor? _selectedSystemMonitor;

        [ObservableProperty]
        private List<string> _processList = SystemProcessHelper.GetProcessWindowTitles();
        [ObservableProperty]
        private string? _selectedProcess;

        [ObservableProperty]
        private IEnumerable<TemplateSearchAreaTypesEnum> _templateSearchAreaTypesEnumValues;
        [ObservableProperty]
        private TemplateSearchAreaTypesEnum _selectedTemplateSearchAreaTypesEnum = TemplateSearchAreaTypesEnum.SELECT_EVERY_MONITOR;

        public TemplateSearchAreaFlowParameterVM(
            FlowsVM flowsViewModel,
            IDataService dataService,
            ISystemService systemService) : base(dataService)
        {
            _dataService = dataService;
            _systemService = systemService;
            _flowsViewModel = flowsViewModel;

            _templateSearchAreaTypesEnumValues = Enum.GetValues(typeof(TemplateSearchAreaTypesEnum)).Cast<TemplateSearchAreaTypesEnum>();
            _systemMonitors = _systemService.GetAllSystemMonitors();
        }

        public override async Task LoadFlowParameterId(int flowParameterId)
        {
            FlowParameter? flowParameter = await _dataService.FlowParameters.FirstOrDefaultAsync(x => x.Id == flowParameterId);
            if (flowParameter != null)
                FlowParameter = flowParameter;

            SelectedProcess = FlowParameter.ProcessName;

            if (FlowParameter.TemplateSearchAreaType != null)
                SelectedTemplateSearchAreaTypesEnum = FlowParameter.TemplateSearchAreaType.Value;
        }


        [RelayCommand]
        private void OnTemplateSearchAreaType()
        {
            switch (SelectedTemplateSearchAreaTypesEnum)
            {
                case TemplateSearchAreaTypesEnum.SELECT_EVERY_MONITOR:
                    Rectangle rect = _systemService.GetScreenSize();
                    SearchAreaRectangle.Top = rect.Top;
                    SearchAreaRectangle.Left = rect.Left;
                    SearchAreaRectangle.Right = rect.Right;
                    SearchAreaRectangle.Bottom = rect.Bottom;
                    break;
                case TemplateSearchAreaTypesEnum.SELECT_MONITOR:
                    OnSystemMonitorChanged();
                    break;
                case TemplateSearchAreaTypesEnum.SELECT_APPLICATION_WINDOW:
                    OnApplicationWindowChanged();
                    break;
                case TemplateSearchAreaTypesEnum.SELECT_CUSTOM_AREA:
                    break;
                default:
                    break;
            }
        }

        [RelayCommand]
        private void OnSystemMonitorChanged()
        {
            SearchAreaRectangle = new SystemMonitor();
            if (SelectedSystemMonitor != null)
            {
                SearchAreaRectangle.Top = SelectedSystemMonitor.Top;
                SearchAreaRectangle.Left = SelectedSystemMonitor.Left;
                SearchAreaRectangle.Right = SelectedSystemMonitor.Right;
                SearchAreaRectangle.Bottom = SelectedSystemMonitor.Bottom;
            }
        }

        [RelayCommand]
        private void OnApplicationWindowChanged()
        {
            SearchAreaRectangle = new SystemMonitor();
            if (SelectedProcess != null)
            {
                Rectangle? windowRect = _systemService.GetWindowSize(SelectedProcess);
                if (windowRect == null)
                    return;

                SearchAreaRectangle.Top = windowRect.Value.Top;
                SearchAreaRectangle.Left = windowRect.Value.Left;
                SearchAreaRectangle.Right = windowRect.Value.Right;
                SearchAreaRectangle.Bottom = windowRect.Value.Bottom;
            }
        }

        public override async Task OnSave()
        {
            // Edit mode
            if (FlowParameter.Id > 0)
            {
                FlowParameter updateFlowParameter = await _dataService.FlowParameters.FirstAsync(x => x.Id == FlowParameter.Id);
                updateFlowParameter.Name = FlowParameter.Name;
                updateFlowParameter.LocationTop = SearchAreaRectangle.Top;
                updateFlowParameter.LocationLeft = SearchAreaRectangle.Left;
                updateFlowParameter.LocationRight = SearchAreaRectangle.Right;
                updateFlowParameter.LocationLeft = SearchAreaRectangle.Left;
                updateFlowParameter.TemplateSearchAreaType = SelectedTemplateSearchAreaTypesEnum;
                updateFlowParameter.ProcessName = SelectedProcess ?? "";
                updateFlowParameter.SystemMonitorDeviceName = SelectedSystemMonitor?.DeviceName ?? "";
                await _dataService.UpdateAsync(updateFlowParameter);
            }

            // Add mode
            else
            {
                if (FlowParameter.ParentFlowParameterId == null)
                    return;

                FlowParameter isNewSimpling = await _dataService.FlowParameters.GetIsNewSibling(FlowParameter.ParentFlowParameterId.Value);
                FlowParameter.OrderingNum = isNewSimpling.OrderingNum;
                isNewSimpling.OrderingNum++;
                await _dataService.UpdateAsync(isNewSimpling);


                if (FlowParameter.Name.Length == 0)
                    FlowParameter.Name = "Template search area parameter.";

                FlowParameter.LocationTop = SearchAreaRectangle.Top;
                FlowParameter.LocationLeft = SearchAreaRectangle.Left;
                FlowParameter.LocationRight = SearchAreaRectangle.Right;
                FlowParameter.LocationLeft = SearchAreaRectangle.Left;
                FlowParameter.TemplateSearchAreaType = SelectedTemplateSearchAreaTypesEnum;
                FlowParameter.ProcessName = SelectedProcess ?? "";
                FlowParameter.SystemMonitorDeviceName = SelectedSystemMonitor?.DeviceName ?? "";

                await _dataService.FlowParameters.AddAsync(FlowParameter);
            }


            _flowsViewModel.RefreshData();
        }
    }
}
