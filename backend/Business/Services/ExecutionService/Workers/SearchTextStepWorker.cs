using System.Drawing;

using Business.Services.AreaPointService;
using Business.Services.OcrService;
using Business.Services.ScreenshotService;
using Core.Enums;
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
    public class SearchTextStepWorker : IStepWorker
    {
        private readonly IScreenshotService _screenshotService;
        private readonly IOcrService _ocrService;
        private readonly IAreaPointResolver _areaPointResolver;

        public SearchTextStepWorker(
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
            if (step.FlowAreaId == null)
                return ExecutionStep.Failure("This step has no area to read.");

            AreaResolution area = await _areaPointResolver.ResolveAreaAsync(step.FlowAreaId.Value, ct);
            if (!area.IsResolved)
                return ExecutionStep.Failure(area.Error);

            if (step.SearchMode == SearchModeEnum.WAIT_UNTIL_FOUND || step.SearchMode == SearchModeEnum.WAIT_UNTIL_NOT_FOUND)
                return await LoopReadAsync(step, area.Bounds, cache, ct);

            return await ReadAsync(step, area.Bounds, cache, ct);
        }


        // ================================================================
        // Private methods
        // ================================================================

        private async Task<ExecutionStep> LoopReadAsync(FlowStep step, Rectangle bounds, IExecutionCacheService cache, CancellationToken ct)
        {
            bool wantSatisfied = step.SearchMode == SearchModeEnum.WAIT_UNTIL_FOUND;
            DateTime giveUpAt = DateTime.UtcNow.AddMilliseconds(step.TimeoutMilliseconds);

            while (true)
            {
                ct.ThrowIfCancellationRequested();

                ExecutionStep read = await ReadAsync(step, bounds, cache, ct);
                bool satisfied = read.Outcome == StepOutcomeEnum.SUCCESS;

                if (satisfied == wantSatisfied)
                    return read;

                // A zero timeout waits for ever.
                if (step.TimeoutMilliseconds > 0 && DateTime.UtcNow >= giveUpAt)
                {
                    // The last read, not a generic message: what the screen actually said when it
                    // gave up is the whole of what a person needs to see.
                    ExecutionStep gaveUp = ExecutionStep.Failure($"Gave up waiting. Last read \"{read.Value}\", against {ConditionEvaluator.Describe(step)}.");
                    gaveUp.Value = read.Value;
                    gaveUp.Screenshot = read.Screenshot;

                    return gaveUp;
                }

                await Task.Delay(step.PollIntervalMilliseconds, ct);
            }
        }

        private async Task<ExecutionStep> ReadAsync(FlowStep step, Rectangle bounds, IExecutionCacheService cache, CancellationToken ct)
        {
            if (bounds.Width <= 0 || bounds.Height <= 0)
                return ExecutionStep.Failure("The area has no size. The window is probably minimised.");

            RawImage image = _screenshotService.CaptureRaw(bounds);

            // The screenshot the text was read from, so a wrong read can be seen rather than
            // guessed at. Whether it is worth keeping is the cache's business.
            ExecutionScreenshot? screenshot = cache.EncodeForHistory(image, step);

            string text = await _ocrService.ReadAsync(image, step.OcrLanguage, ct);
            string value = TextExtractHelper.Extract(text, step.ResultExtractPattern);

            ExecutionStep result = ConditionEvaluator.IsSatisfied(value, step.ConditionType, step.ConditionText, step.ConditionTextEnd)
                                   ? ExecutionStep.Success(Centre(bounds))
                                   : ExecutionStep.Failure($"Read \"{value}\", which does not satisfy {ConditionEvaluator.Describe(step)}.");

            result.Value = value;
            result.Screenshot = screenshot;

            return result;
        }

        private static Point Centre(Rectangle bounds)
        {
            return new Point(bounds.Left + bounds.Width / 2, bounds.Top + bounds.Height / 2);
        }
    }
}
