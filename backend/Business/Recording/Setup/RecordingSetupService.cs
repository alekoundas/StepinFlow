using Business.AreaPoint;
using Business.Command;
using Business.Flows.DataService;
using Core.Enums;
using Core.Helpers;
using Core.Models.Business.AreaPointResolution;
using Core.Models.Database;
using Core.Models.Dtos;
using Core.Ports;

namespace Business.Recording.Setup
{
    /// <summary>
    /// The flow a recording goes into, made before the first click: its name, description, main
    /// area and whether it is a QA test.
    ///
    /// The launch step starts the main area, which is started by the program its window belongs to
    /// now, so the window has to be open. It is the program file on this machine, or a Steam game's
    /// link, which the tester can edit on the area.
    /// </summary>
    public sealed class RecordingSetupService : IRecordingSetupService
    {
        private const string MAIN_AREA_NAME = "Main window";

        private readonly DataService _dataService;
        private readonly IAreaPointResolver _areaPointResolver;
        private readonly IWindowService _windowService;
        private readonly IProcessService _processService;

        public RecordingSetupService(
            DataService dataService,
            IAreaPointResolver areaPointResolver,
            IWindowService windowService,
            IProcessService processService)
        {
            _dataService = dataService;
            _areaPointResolver = areaPointResolver;
            _windowService = windowService;
            _processService = processService;
        }


        // ================================================================
        // Public methods
        // ================================================================

        public async Task<ResultDto<int>> CreateFlowAsync(RecordingSetupDto dto, CancellationToken ct)
        {
            string name = dto.Name.Trim();
            string? invalidName = FileNameHelper.Validate(name);
            if (invalidName != null)
                return ResultDto<int>.Failure(invalidName);

            // Matching on nothing would take the first window on the desktop.
            if (string.IsNullOrWhiteSpace(dto.ProcessName) && string.IsNullOrWhiteSpace(dto.TitlePattern))
                return ResultDto<int>.Failure("Pick the application the flow works in.");

            FlowArea mainArea = new FlowArea
            {
                Name = MainAreaName(dto.ProcessName),
                Type = FlowAreaTypeEnum.APPLICATION,
                IsMain = true,
                ProcessName = dto.ProcessName.Trim(),
                TitlePattern = dto.TitlePattern.Trim(),
                TitleMatchMode = dto.TitleMatchMode,
                UseClientArea = dto.UseClientArea,
                ScalesWith = dto.ScalesWith,
            };

            FlowStep? launch = null;
            if (dto.AddsLaunchStep)
            {
                AreaResolution area = _areaPointResolver.ResolveArea(mainArea);
                if (!area.IsResolved)
                    return ResultDto<int>.Failure($"{area.Error} Open the application, or untick the launch step.");

                string? program = _processService.GetExecutablePath(_windowService.GetProcessId(area.Window));
                if (string.IsNullOrEmpty(program))
                    return ResultDto<int>.Failure("Could not read which program the window belongs to. It may be running as administrator. Untick the launch step, and add one by hand.");

                // The area says what starts it, so the launch names only the area.
                mainArea.LaunchCommand = LaunchCommandHelper.Of(program);
                launch = new FlowStep
                {
                    Name = $"Launch {mainArea.Name}",
                    FlowStepType = FlowStepTypeEnum.SYSTEM_COMMAND,
                    RunCommandPreset = RunCommandPresetEnum.LAUNCH_APP,
                    FlowArea = mainArea,
                };
            }

            Flow flow = new Flow
            {
                Name = name,
                Description = dto.Description.Trim(),
                IsSubFlow = dto.IsSubFlow,
                IsQaTest = dto.IsQaTest,
            };

            return await _dataService.Flow.CreateForRecordingAsync(flow, mainArea, launch, ct);
        }


        // ================================================================
        // Private methods
        // ================================================================

        private static string MainAreaName(string processName)
        {
            if (string.IsNullOrWhiteSpace(processName))
                return MAIN_AREA_NAME;

            return processName.Trim();
        }
    }
}
