using System.Drawing;

using Business.AreaPoint;
using Core.Ports;
using Core.Enums;
using Core.Helpers;
using Core.Models.Business;
using Core.Models.Database;
using Core.Models.Business.AreaPointResolution;

namespace Business.Executions.Workers
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
        private readonly TimeProvider _timeProvider;

        public SearchTextStepWorker(
            IScreenshotService screenshotService,
            IOcrService ocrService,
            IAreaPointResolver areaPointResolver,
            TimeProvider timeProvider)
        {
            _screenshotService = screenshotService;
            _ocrService = ocrService;
            _areaPointResolver = areaPointResolver;
            _timeProvider = timeProvider;
        }

        public async Task<ExecutionStep> ExecuteAsync(FlowStep step, IExecutionCacheService cache, CancellationToken ct)
        {
            if (step.FlowAreaId == null)
                return ExecutionStep.Failure("This step has no area to read.");

            // A value a step above was to set and did not is the flow's mistake, and no try mends it.
            VariableTranslationResult expected = cache.TranslateVariables(step.ConditionText);
            VariableTranslationResult expectedEnd = cache.TranslateVariables(step.ConditionTextEnd);

            if (!expected.IsTranslated || !expectedEnd.IsTranslated)
                return ExecutionStep.Failure(VariableTranslator.DescribeUntranslated([.. expected.Untranslated, .. expectedEnd.Untranslated]));

            if (step.SearchMode == SearchModeEnum.WAIT_UNTIL_FOUND || step.SearchMode == SearchModeEnum.WAIT_UNTIL_NOT_FOUND)
                return await LoopReadAsync(step, step.FlowAreaId.Value, expected.Text, expectedEnd.Text, cache, ct);

            AreaResolution area = await _areaPointResolver.ResolveAreaAsync(step.FlowAreaId.Value, ct);

            string? cannotRead = CannotRead(area);
            if (cannotRead != null)
                return ExecutionStep.Failure(cannotRead);

            return await ReadAsync(step, area, expected.Text, expectedEnd.Text, cache, ct);
        }


        // ================================================================
        // Private methods
        // ================================================================

        // The area is found again on every try: the window may open, move or change size while this
        // waits. A try that cannot read - no window yet, a minimised one - is neither satisfied nor
        // not, and the next one may.
        private async Task<ExecutionStep> LoopReadAsync(FlowStep step, int flowAreaId, string expected, string expectedEnd, IExecutionCacheService cache, CancellationToken ct)
        {
            bool wantSatisfied = step.SearchMode == SearchModeEnum.WAIT_UNTIL_FOUND;
            DateTime giveUpAt = _timeProvider.GetUtcNow().UtcDateTime.AddMilliseconds(step.TimeoutMilliseconds);
            ExecutionStep? lastRead = null;

            while (true)
            {
                ct.ThrowIfCancellationRequested();

                AreaResolution area = await _areaPointResolver.ResolveAreaAsync(flowAreaId, ct);
                if (area.Unresolved == AreaUnresolvedEnum.NOT_SET_UP)
                    return ExecutionStep.Failure(area.Error);

                // What it waits to see go was in that window, so a window that has closed took it.
                if (!wantSatisfied && area.Unresolved == AreaUnresolvedEnum.NOT_ON_SCREEN)
                    return ExecutionStep.Success(message: $"No longer on screen: {area.Error}");

                string? cannotRead = CannotRead(area);
                if (cannotRead == null)
                {
                    ExecutionStep read = await ReadAsync(step, area, expected, expectedEnd, cache, ct);
                    bool satisfied = read.Outcome == StepOutcomeEnum.SUCCESS;

                    if (satisfied && wantSatisfied)
                        return read;

                    // No longer saying it is what this mode waits for, so it is a success - the read
                    // itself failed its condition, which on its own reads as a failure. It is a
                    // condition and nothing more, so the read goes in the message and not the value.
                    if (!satisfied && !wantSatisfied)
                    {
                        ExecutionStep gone = ExecutionStep.Success(message: $"No longer {ConditionEvaluatorHelper.Describe(step)}. Last read \"{read.Value}\".");
                        gone.Screenshot = read.Screenshot;

                        return gone;
                    }

                    lastRead = read;
                }

                // A zero timeout waits for ever.
                if (step.TimeoutMilliseconds > 0 && _timeProvider.GetUtcNow().UtcDateTime >= giveUpAt)
                    return GaveUp(step, lastRead, cannotRead);

                await Task.Delay(step.PollIntervalMilliseconds, ct);
            }
        }

        // The last read, not a generic message: what the screen actually said when it gave up is the
        // whole of what a person needs to see - and why the last try could not read, if it could not.
        private static ExecutionStep GaveUp(FlowStep step, ExecutionStep? lastRead, string? cannotRead)
        {
            string message = "Gave up waiting.";
            if (lastRead != null)
                message += $" Last read \"{lastRead.Value}\", against {ConditionEvaluatorHelper.Describe(step)}.";

            if (cannotRead != null)
                message += $" The last try could not read: {cannotRead}";

            ExecutionStep gaveUp = ExecutionStep.Failure(message);
            gaveUp.Value = lastRead?.Value;
            gaveUp.Screenshot = lastRead?.Screenshot;

            return gaveUp;
        }

        private static string? CannotRead(AreaResolution area)
        {
            if (!area.IsResolved)
                return area.Error;

            if (area.Bounds.Width <= 0 || area.Bounds.Height <= 0)
                return "The area has no size. The window is probably minimised.";

            return null;
        }

        private async Task<ExecutionStep> ReadAsync(FlowStep step, AreaResolution area, string expected, string expectedEnd, IExecutionCacheService cache, CancellationToken ct)
        {
            RawImage image = _screenshotService.CaptureRaw(area.Bounds);

            // The screenshot the text was read from, so a wrong read can be seen rather than
            // guessed at. Whether it is worth keeping is the cache's business.
            ExecutionScreenshot? screenshot = cache.EncodeForHistory(image, step);

            string text = await _ocrService.ReadAsync(image, step.OcrLanguage, area.Dpi, ct);
            string value = RegexHelper.Extract(text, step.ResultExtractPattern);

            ExecutionStep result = ConditionEvaluatorHelper.IsSatisfied(value, step.ConditionType, expected, expectedEnd)
                                   ? ExecutionStep.Success(Centre(area.Bounds))
                                   : ExecutionStep.Failure($"Read \"{value}\", which does not satisfy {ConditionEvaluatorHelper.Describe(step)}.");

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
