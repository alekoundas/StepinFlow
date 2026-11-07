using Business.AreaPoint;
using Business.Shell;
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
    /// area and whether it is a QA test, and the steps that ready its window.
    ///
    /// The main area is an application's window or a website's tab in a browser. The launch step
    /// starts the main area, which is started by the program its window belongs to now, so the
    /// window has to be open. It is the program file on this machine, or a Steam game's link,
    /// which the tester can edit on the area; a website's is its browser, which Launch opens at
    /// the address when no tab has it.
    ///
    /// The resize step sets the window's size, and the setup sets it there and then, so what is
    /// recorded next is at the size every execution sets.
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

            string? unmatched = Unmatched(dto);
            if (unmatched != null)
                return ResultDto<int>.Failure(unmatched);

            if (dto.AddsResizeStep && (dto.WindowWidth < 1 || dto.WindowHeight < 1))
                return ResultDto<int>.Failure("Give the window a width and a height, or untick the resize step.");

            FlowArea mainArea = MainAreaOf(dto);
            List<FlowStep> steps = [];

            if (dto.AddsLaunchStep)
            {
                AreaResolution area = _areaPointResolver.ResolveArea(mainArea);
                if (!area.IsResolved)
                    return ResultDto<int>.Failure($"{area.Error} {OpenIt(mainArea)}, or untick the launch step.");

                string? program = _processService.GetExecutablePath(_windowService.GetProcessId(area.Window));
                if (string.IsNullOrEmpty(program))
                    return ResultDto<int>.Failure("Could not read which program the window belongs to. It may be running as administrator. Untick the launch step, and add one by hand.");

                // The area says what starts it, so the launch names only the area.
                mainArea.LaunchCommand = LaunchCommandHelper.Of(program);
                steps.Add(new FlowStep
                {
                    Name = $"Launch {mainArea.Name}",
                    FlowStepType = FlowStepTypeEnum.SYSTEM_COMMAND,
                    ShellCommandMode = ShellCommandModeEnum.LAUNCH_APP,
                    FlowArea = mainArea,
                });
            }

            // Last of the checks, since it changes the window on the screen.
            if (dto.AddsResizeStep)
            {
                AreaResolution area = _areaPointResolver.ResolveArea(mainArea);
                if (!area.IsResolved)
                    return ResultDto<int>.Failure($"{area.Error} {OpenIt(mainArea)}, or untick the resize step.");

                string? refused = RecordingWindowSize.Apply(_windowService, area.Window, dto.WindowWidth, dto.WindowHeight);
                if (refused != null)
                    return ResultDto<int>.Failure($"{refused} Restore it if it is maximised or full screen, or untick the resize step.");

                // Named by its size, as every window step is.
                steps.Add(new FlowStep
                {
                    FlowStepType = FlowStepTypeEnum.WINDOW_RESIZE,
                    WindowWidth = dto.WindowWidth,
                    WindowHeight = dto.WindowHeight,
                    FlowArea = mainArea,
                });
            }

            Flow flow = new Flow
            {
                Name = name,
                Description = dto.Description.Trim(),
                IsSubFlow = dto.IsSubFlow,
                IsQaTest = dto.IsQaTest,
            };

            return await _dataService.Flow.CreateForRecordingAsync(flow, mainArea, steps, ct);
        }


        // ================================================================
        // Private methods
        // ================================================================

        // Matching on nothing would take the first window on the desktop.
        private static string? Unmatched(RecordingSetupDto dto)
        {
            if (dto.AreaType == FlowAreaTypeEnum.APPLICATION)
            {
                if (string.IsNullOrWhiteSpace(dto.ProcessName) && string.IsNullOrWhiteSpace(dto.TitlePattern))
                    return "Pick the application the flow works in.";

                return null;
            }

            if (dto.AreaType == FlowAreaTypeEnum.BROWSER_TAB)
            {
                if (string.IsNullOrWhiteSpace(dto.ProcessName))
                    return "Pick the browser the website is open in.";

                if (string.IsNullOrWhiteSpace(dto.TabAddress))
                    return "Type the website's address.";

                return null;
            }

            return "A recording works in an application's window or a website's tab.";
        }

        // A tab names no title: a browser's is its front tab's, so it would only match while that tab was in front.
        private static FlowArea MainAreaOf(RecordingSetupDto dto)
        {
            FlowArea area = new FlowArea
            {
                Type = dto.AreaType,
                IsMain = true,
                ProcessName = dto.ProcessName.Trim(),
                TitleMatchMode = dto.TitleMatchMode,
                UseClientArea = dto.UseClientArea,
                ScalesWith = dto.ScalesWith,
            };

            if (dto.AreaType == FlowAreaTypeEnum.BROWSER_TAB)
            {
                area.TabAddress = dto.TabAddress.Trim();
                area.Name = WebsiteName(area.TabAddress);
                return area;
            }

            area.TitlePattern = dto.TitlePattern.Trim();
            area.Name = ApplicationName(area.ProcessName);
            return area;
        }

        private static string ApplicationName(string processName)
        {
            if (processName.Length == 0)
                return MAIN_AREA_NAME;

            return processName;
        }

        // shop.example.com/cart, as the address bar shows it.
        private static string WebsiteName(string address)
        {
            string name = address;

            int scheme = name.IndexOf("://", StringComparison.Ordinal);
            if (scheme >= 0)
                name = name[(scheme + 3)..];

            if (name.StartsWith("www.", StringComparison.OrdinalIgnoreCase))
                name = name[4..];

            name = name.TrimEnd('/');
            if (name.Length == 0)
                return MAIN_AREA_NAME;

            return name;
        }

        private static string OpenIt(FlowArea mainArea)
        {
            if (mainArea.Type == FlowAreaTypeEnum.BROWSER_TAB)
                return "Open the website in its browser, in front";

            return "Open the application";
        }
    }
}
