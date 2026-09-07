using System.Drawing;

using Business.Services.AreaPointService;
using Business.Services.ScreenshotService;
using Core.Models.Business;
using Core.Models.Database;

namespace Business.Services.ExecutionService.Workers
{
    /// <summary>
    /// Takes one screenshot and names it, so several conditions can read the same instant.
    ///
    /// Checking two things against two screenshots taken a moment apart answers a question nobody
    /// asked. Capturing once and deciding twice answers the one that was.
    /// </summary>
    public class CaptureScreenStepWorker : IStepWorker
    {
        private readonly IScreenshotService _screenshotService;
        private readonly IAreaPointResolver _areaPointResolver;

        public CaptureScreenStepWorker(
            IScreenshotService screenshotService,
            IAreaPointResolver areaPointResolver)
        {
            _screenshotService = screenshotService;
            _areaPointResolver = areaPointResolver;
        }

        public async Task<ExecutionStep> ExecuteAsync(FlowStep step, IExecutionCacheService cache, CancellationToken ct)
        {
            // Validation.
            if (step.FlowAreaId == null)
                return ExecutionStep.Failure("This step has nothing to capture: give it an area.");

            AreaResolution area = await _areaPointResolver.ResolveAreaAsync(step.FlowAreaId.Value, ct);
            if (!area.IsResolved)
                return ExecutionStep.Failure(area.Error);

            Rectangle bounds = area.Bounds;
            if (bounds.Width <= 0 || bounds.Height <= 0)
                return ExecutionStep.Failure("The area has no size. The window is probably minimised.");

            // Execution.
            RawImage image = _screenshotService.CaptureRaw(bounds);
            cache.RecordScreenshot(step.Id, image, bounds);

            ExecutionStep result = ExecutionStep.Success(message: $"Captured {bounds.Width}x{bounds.Height}.");
            result.Screenshot = cache.EncodeForHistory(image, step);

            return result;
        }
    }
}
