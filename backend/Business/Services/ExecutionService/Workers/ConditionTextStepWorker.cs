using System.Drawing;

using Business.Services.AreaPointService;
using Business.Services.OcrService;
using Business.Services.ScreenshotService;
using Core.Helpers;
using Core.Models.Business;
using Core.Models.Database;

namespace Business.Services.ExecutionService.Workers
{
    /// <summary>
    /// Reads what is on screen and decides whether it says what it should.
    ///
    /// The read is kept whether or not the check passes: a step below reads it, and a failure that
    /// says what was actually on screen is worth a great deal more than one that says it failed.
    /// </summary>
    public class ConditionTextStepWorker : IStepWorker
    {
        private readonly IScreenshotService _screenshotService;
        private readonly IOcrService _ocrService;
        private readonly IAreaPointResolver _areaPointResolver;

        public ConditionTextStepWorker(
            IScreenshotService screenshotService,
            IOcrService ocrService,
            IAreaPointResolver areaPointResolver)
        {
            _screenshotService = screenshotService;
            _ocrService = ocrService;
            _areaPointResolver = areaPointResolver;
        }

        public async Task<ExecutionStep> ExecuteAsync(FlowStep step, IExecutionCacheService cache, CancellationToken ct)
        {
            // Validate.
            if (step.FlowAreaId == null)
                return ExecutionStep.Failure("This step has no area to read.");

            AreaResolution area = await _areaPointResolver.ResolveAreaAsync(step.FlowAreaId.Value, ct);
            if (!area.IsResolved)
                return ExecutionStep.Failure(area.Error);

            if (area.Bounds.Width <= 0 || area.Bounds.Height <= 0)
                return ExecutionStep.Failure("The area has no size. The window is probably minimised.");

            // Execute.
            RawImage image = _screenshotService.CaptureRaw(area.Bounds);
            string text = await _ocrService.ReadAsync(image, step.OcrLanguage, ct);
            string value = TextExtractHelper.Extract(text, step.ResultExtractPattern);

            if (!ConditionEvaluator.IsSatisfied(value, step.ConditionType, step.ConditionText, step.ConditionTextEnd))
            {
                ExecutionStep unsatisfied = ExecutionStep.Failure($"Read \"{value}\", which does not satisfy {ConditionEvaluator.Describe(step)}.");
                unsatisfied.Value = value;
                return unsatisfied;
            }

            Point centre = new Point(area.Bounds.Left + area.Bounds.Width / 2, area.Bounds.Top + area.Bounds.Height / 2);

            ExecutionStep satisfied = ExecutionStep.Success(centre);
            satisfied.Value = value;
            return satisfied;
        }
    }
}
