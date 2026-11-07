using System.Globalization;

using Business.AreaPoint;
using Core.Ports;
using Core.Enums;
using Core.Enums.Business;
using Core.Models.Business;
using Core.Models.Database;
using Core.Models.Business.AreaPointResolution;

namespace Business.Executions.Workers
{
    /// <summary>
    /// Focus, resize, move and close the window of the step's area. A browser tab's window is the
    /// one holding it, and its tab is brought to the front of it first.
    ///
    /// A close asks, the way the window's X does, and waits for it to go. Forced, a window still
    /// open after that has its program ended. A tab is closed the way Ctrl+W closes it, and never
    /// forced: its program is the whole browser.
    /// </summary>
    public class WindowStepWorker : IStepWorker
    {
        private static readonly TimeSpan CLOSE_TIMEOUT = TimeSpan.FromSeconds(5);
        private static readonly TimeSpan FORCED_TIMEOUT = TimeSpan.FromSeconds(5);
        private static readonly TimeSpan CLOSE_POLL_INTERVAL = TimeSpan.FromMilliseconds(100);

        private readonly IAreaPointResolver _areaPointResolver;
        private readonly IWindowService _windowService;
        private readonly BrowserTabFinder _browserTabFinder;
        private readonly IProcessService _processService;
        private readonly IInputSimulatorService _inputSimulatorService;
        private readonly TimeProvider _timeProvider;

        public WindowStepWorker(
            IAreaPointResolver areaPointResolver,
            IWindowService windowService,
            BrowserTabFinder browserTabFinder,
            IProcessService processService,
            IInputSimulatorService inputSimulatorService,
            TimeProvider timeProvider)
        {
            _areaPointResolver = areaPointResolver;
            _windowService = windowService;
            _browserTabFinder = browserTabFinder;
            _processService = processService;
            _inputSimulatorService = inputSimulatorService;
            _timeProvider = timeProvider;
        }

        public async Task<ExecutionStep> ExecuteAsync(FlowStep step, IExecutionCacheService cache, CancellationToken ct)
        {
            FlowArea? area = step.FlowArea;
            if (area == null)
                return ExecutionStep.Failure("There is no window to act on.");

            if (area.Type != FlowAreaTypeEnum.APPLICATION && area.Type != FlowAreaTypeEnum.BROWSER_TAB)
                return ExecutionStep.Failure(Detail(area, "not a window"));

            WindowHandle window = await WindowOfAsync(area, ct);
            if (!window.IsValid && area.Type == FlowAreaTypeEnum.BROWSER_TAB)
                return ExecutionStep.Failure(Detail(area, $"no {area.ProcessName} tab is at {area.TabAddress}"));

            if (!window.IsValid)
                return ExecutionStep.Failure(Detail(area, "no window matched"));

            switch (step.FlowStepType)
            {
                case FlowStepTypeEnum.WINDOW_FOCUS:
                    return Focus(area, window);

                case FlowStepTypeEnum.WINDOW_RESIZE:
                    return Resize(step, area, window);

                case FlowStepTypeEnum.WINDOW_RELOCATE:
                    return await RelocateAsync(step, area, window, ct);

                case FlowStepTypeEnum.WINDOW_CLOSE when area.Type == FlowAreaTypeEnum.BROWSER_TAB:
                    return await CloseTabAsync(step, area, window, ct);

                case FlowStepTypeEnum.WINDOW_CLOSE:
                    return await CloseAsync(step, area, window, ct);

                default:
                    return ExecutionStep.Failure($"{step.FlowStepType} is not a window step.");
            }
        }


        // ================================================================
        // Private methods
        // ================================================================

        private async Task<WindowHandle> WindowOfAsync(FlowArea area, CancellationToken ct)
        {
            if (area.Type == FlowAreaTypeEnum.BROWSER_TAB)
                return (await _browserTabFinder.BringToFrontAsync(area, ct)).Window;

            WindowQuery query = new WindowQuery
            {
                ProcessName = area.ProcessName,
                TitlePattern = area.TitlePattern,
                TitleMatchMode = area.TitleMatchMode,
                IgnoreWindowBorders = false,
            };

            return _windowService.FindWindow(query);
        }

        private ExecutionStep Focus(FlowArea area, WindowHandle window)
        {
            if (!_windowService.FocusWindow(window))
                return ExecutionStep.Failure(Detail(area, "the window would not come to the front"));

            return ExecutionStep.Success(message: Detail(area, "focused"));
        }

        private ExecutionStep Resize(FlowStep step, FlowArea area, WindowHandle window)
        {
            if (step.WindowWidth < 1 || step.WindowHeight < 1)
                return ExecutionStep.Failure(Detail(area, "no size to resize to"));

            if (!_windowService.ResizeWindow(window, step.WindowWidth, step.WindowHeight))
                return ExecutionStep.Failure(Detail(area, "the window would not resize"));

            return ExecutionStep.Success(message: Detail(area, $"{step.WindowWidth} x {step.WindowHeight}"));
        }

        private async Task<ExecutionStep> RelocateAsync(FlowStep step, FlowArea area, WindowHandle window, CancellationToken ct)
        {
            if (step.FlowPointId == null)
                return ExecutionStep.Failure(Detail(area, "nowhere to move to"));

            PointResolution point = await _areaPointResolver.ResolvePointAsync(step.FlowPointId.Value, ct);
            if (!point.IsResolved)
                return ExecutionStep.Failure(point.Error);

            if (!_windowService.MoveWindow(window, point.Point.X, point.Point.Y))
                return ExecutionStep.Failure(Detail(area, "the window would not move"));

            return ExecutionStep.Success(point.Point, Detail(area, "moved"));
        }

        private async Task<ExecutionStep> CloseAsync(FlowStep step, FlowArea area, WindowHandle window, CancellationToken ct)
        {
            TimeSpan timeout = CloseTimeoutOf(step);

            // Asked first: a window that has gone has no program to ask about.
            int processId = _windowService.GetProcessId(window);

            if (!_windowService.CloseWindow(window))
                return ExecutionStep.Failure(Detail(area, "the window would not take the close"));

            if (await IsGoneWithinAsync(() => !_windowService.IsWindowOpen(window), timeout, ct))
                return ExecutionStep.Success(message: Detail(area, "closed"));

            string stillOpen = $"still open after {Seconds(timeout)}, often asking whether to save";
            if (!step.IsCloseForced)
                return ExecutionStep.Failure(Detail(area, stillOpen));

            if (processId == 0 || !_processService.Kill(processId))
                return ExecutionStep.Failure(Detail(area, $"{stillOpen}, and its program would not end"));

            if (await IsGoneWithinAsync(() => !_windowService.IsWindowOpen(window), FORCED_TIMEOUT, ct))
                return ExecutionStep.Success(message: Detail(area, $"{stillOpen}, so its program was ended"));

            return ExecutionStep.Failure(Detail(area, $"{stillOpen}, and still there once its program was ended"));
        }

        // Ctrl+W goes to whatever is in front, so only once the browser is, with the tab in front of it.
        private async Task<ExecutionStep> CloseTabAsync(FlowStep step, FlowArea area, WindowHandle window, CancellationToken ct)
        {
            TimeSpan timeout = CloseTimeoutOf(step);

            if (!_windowService.FocusWindow(window))
                return ExecutionStep.Failure(Detail(area, "the browser would not come to the front, so its tab was left open"));

            int tabs = _browserTabFinder.TabCount(window);
            _inputSimulatorService.SimulateKeyCombination([KeyCodeEnum.LeftCtrl], KeyCodeEnum.W);

            if (await IsGoneWithinAsync(() => IsTabGone(area, window, tabs), timeout, ct))
                return ExecutionStep.Success(message: Detail(area, "tab closed"));

            return ExecutionStep.Failure(Detail(area, $"the tab was still open after {Seconds(timeout)}, often asking whether to leave the page"));
        }

        // Gone with its window, one tab fewer, or - when the tab strip cannot be read - its address no
        // longer in front.
        private bool IsTabGone(FlowArea area, WindowHandle window, int tabsBefore)
        {
            if (!_windowService.IsWindowOpen(window))
                return true;

            if (tabsBefore > 0)
                return _browserTabFinder.TabCount(window) < tabsBefore;

            return _browserTabFinder.FindInFront(area) != window;
        }

        private async Task<bool> IsGoneWithinAsync(Func<bool> isGone, TimeSpan timeout, CancellationToken ct)
        {
            DateTimeOffset giveUpAt = _timeProvider.GetUtcNow() + timeout;

            while (!isGone())
            {
                if (_timeProvider.GetUtcNow() >= giveUpAt)
                    return false;

                await Task.Delay(CLOSE_POLL_INTERVAL, _timeProvider, ct);
            }

            return true;
        }

        private static TimeSpan CloseTimeoutOf(FlowStep step)
        {
            if (step.TimeoutMilliseconds > 0)
                return TimeSpan.FromMilliseconds(step.TimeoutMilliseconds);

            return CLOSE_TIMEOUT;
        }

        private static string Seconds(TimeSpan timeout)
        {
            return $"{timeout.TotalSeconds.ToString("0.#", CultureInfo.InvariantCulture)}s";
        }

        // Says which window was being looked for, since that is what usually went wrong.
        private static string Detail(FlowArea area, string outcome)
        {
            return $"{area.Name} - {outcome}";
        }
    }
}
