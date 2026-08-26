using System.Drawing;
using System.Text.RegularExpressions;
using Business.Services.AreaPointService;
using Business.Services.OcrService;
using Business.Services.ScreenshotService;
using Core.Helpers;
using Core.Models.Business;
using Core.Models.Database;

namespace Business.Services.ExecutionService.Workers
{
    public class ReadTextStepWorker : IStepWorker
    {
        private readonly IScreenshotService _screenshotService;
        private readonly IOcrService _ocrService;
        private readonly IAreaPointResolver _areaPointResolver;

        public ReadTextStepWorker(
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

            if (area.Bounds.Width <= 0 || area.Bounds.Height <= 0)
                return ExecutionStep.Failure("The area has no size. The window is probably minimised.");

            RawImage image = _screenshotService.CaptureRaw(area.Bounds);
            string text = await _ocrService.ReadAsync(image, step.OcrLanguage, ct);

            string value = Extract(text, step.ResultExtractPattern);
            bool satisfied = ConditionEvaluator.IsSatisfied(value, step.ConditionType, step.ConditionText, step.ConditionTextEnd);

            // The read is kept either way: a step below reads it through FlowStepReferenceId, and a
            // failure is a great deal easier to understand when it says what was actually on screen.
            if (!satisfied)
            {
                ExecutionStep unsatisfied = ExecutionStep.Failure($"Read \"{value}\", which does not satisfy {ConditionEvaluator.Describe(step)}.");
                unsatisfied.Value = value;
                return unsatisfied;
            }

            Point centre = new Point(area.Bounds.Left + area.Bounds.Width / 2, area.Bounds.Top + area.Bounds.Height / 2);

            ExecutionStep read = ExecutionStep.Success(centre);
            read.Value = value;
            return read;
        }


        // ================================================================
        // Private methods
        // ================================================================

        private static string Extract(string text, string pattern)
        {
            if (string.IsNullOrWhiteSpace(pattern))
                return text;

            try
            {
                Match match = Regex.Match(text, pattern, RegexOptions.None, TimeSpan.FromMilliseconds(200));
                if (!match.Success)
                    return string.Empty;

                return match.Groups.Count > 1 ? match.Groups[1].Value : match.Value;
            }
            catch (ArgumentException)
            {
                return text;
            }
            catch (RegexMatchTimeoutException)
            {
                return text;
            }
        }
    }
}
