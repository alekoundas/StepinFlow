using Business.AreaPoint;
using Core.Ports;
using Core.Enums;
using Core.Models.Business;
using Core.Models.Database;
using Core.Models.Business.AreaPointResolution;

namespace Business.Executions.Workers
{
    /// <summary>
    /// Focus, resize and move the window of the step's area. 
    /// </summary>
    public class WindowStepWorker : IStepWorker
    {
        private readonly IAreaPointResolver _areaPointResolver;
        private readonly IWindowService _windowService;

        public WindowStepWorker(IAreaPointResolver areaPointResolver, IWindowService windowService)
        {
            _areaPointResolver = areaPointResolver;
            _windowService = windowService;
        }

        public async Task<ExecutionStep> ExecuteAsync(FlowStep step, IExecutionCacheService cache, CancellationToken ct)
        {
            FlowArea? area = step.FlowArea;
            if (area == null)
                return ExecutionStep.Failure("There is no window to act on.");

            if (area.Type != FlowAreaTypeEnum.APPLICATION)
                return ExecutionStep.Failure(Detail(area, "not a window"));

            WindowQuery query = new WindowQuery
            {
                ProcessName = area.ProcessName,
                TitlePattern = area.TitlePattern,
                TitleMatchMode = area.TitleMatchMode,
                UseClientArea = false,
            };

            WindowHandle window = _windowService.FindWindow(query);
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

                default:
                    return ExecutionStep.Failure($"{step.FlowStepType} is not a window step.");
            }
        }


        // ================================================================
        // Private methods
        // ================================================================

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

        // Says which window was being looked for, since that is what usually went wrong.
        private static string Detail(FlowArea area, string outcome)
        {
            return $"{area.Name} - {outcome}";
        }
    }
}
