using System.Drawing;

using Business.Services.AreaPointService;
using Business.Services.ScreenshotService;
using Core.Enums;
using Core.Models.Business;
using Core.Models.Database;

namespace Business.Services.ExecutionService.Workers
{
    /// <summary>
    /// Focus, resize and move. 
    /// </summary>
    public class WindowStepWorker : IStepWorker
    {
        private readonly IAreaPointResolver _areaPointResolver;

        public WindowStepWorker(IAreaPointResolver areaPointResolver)
        {
            _areaPointResolver = areaPointResolver;
        }

        public async Task<ExecutionStep> ExecuteAsync(FlowStep step, IExecutionCacheService cache, CancellationToken ct)
        {
            WindowQuery query = new WindowQuery
            {
                ProcessName = step.ProcessName,
                TitlePattern = step.TitlePattern,
                TitleMatchMode = step.TitleMatchMode,
                UseClientArea = false,
            };

            IntPtr window = AppWindowHelper.FindWindow(query);
            if (window == IntPtr.Zero)
                return ExecutionStep.Failure(Detail(step, "no window matched"));

            switch (step.FlowStepType)
            {
                case FlowStepTypeEnum.WINDOW_FOCUS:
                    return Focus(step, window);

                case FlowStepTypeEnum.WINDOW_RESIZE:
                    return Resize(step, window);

                case FlowStepTypeEnum.WINDOW_RELOCATE:
                    return await RelocateAsync(step, window, ct);

                default:
                    return ExecutionStep.Failure($"{step.FlowStepType} is not a window step.");
            }
        }


        // ================================================================
        // Private methods
        // ================================================================

        private static ExecutionStep Focus(FlowStep step, IntPtr window)
        {
            if (!AppWindowHelper.FocusWindow(window))
                return ExecutionStep.Failure(Detail(step, "the window would not come to the front"));

            return ExecutionStep.Success(message: Detail(step, "focused"));
        }

        private static ExecutionStep Resize(FlowStep step, IntPtr window)
        {
            if (step.WindowWidth < 1 || step.WindowHeight < 1)
                return ExecutionStep.Failure(Detail(step, "no size to resize to"));

            if (!AppWindowHelper.ResizeWindow(window, step.WindowWidth, step.WindowHeight))
                return ExecutionStep.Failure(Detail(step, "the window would not resize"));

            return ExecutionStep.Success(message: Detail(step, $"{step.WindowWidth} x {step.WindowHeight}"));
        }

        private async Task<ExecutionStep> RelocateAsync(FlowStep step, IntPtr window, CancellationToken ct)
        {
            if (step.FlowPointId == null)
                return ExecutionStep.Failure(Detail(step, "nowhere to move to"));

            PointResolution point = await _areaPointResolver.ResolvePointAsync(step.FlowPointId.Value, ct);
            if (!point.IsResolved)
                return ExecutionStep.Failure(point.Error);

            if (!AppWindowHelper.MoveWindow(window, point.Point.X, point.Point.Y))
                return ExecutionStep.Failure(Detail(step, "the window would not move"));

            return ExecutionStep.Success(point.Point, Detail(step, "moved"));
        }

        /// <summary>Says which window was being looked for, since that is what usually went wrong.</summary>
        private static string Detail(FlowStep step, string outcome)
        {
            if (!string.IsNullOrWhiteSpace(step.ProcessName))
                return $"{step.ProcessName} - {outcome}";

            if (!string.IsNullOrWhiteSpace(step.TitlePattern))
                return $"Title {step.TitleMatchMode} \"{step.TitlePattern}\" - {outcome}";

            return outcome;
        }
    }
}
