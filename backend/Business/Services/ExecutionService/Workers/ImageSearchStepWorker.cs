using System.Drawing;

using Business.Services.AreaPointService;
using Business.Services.MatchService;
using Business.Services.ScreenshotService;
using Core.Enums;
using Core.Models.Business;
using Core.Models.Database;

namespace Business.Services.ExecutionService.Workers
{
    /// <summary>
    /// Looks for the step's templates in its search area.
    ///
    /// One screenshot per attempt, and every template matched against it.
    /// FIND_ALL returns every hit from that one screenshot rather than searching again between them.
    /// </summary>
    public class ImageSearchStepWorker : IStepWorker
    {
        private readonly IScreenshotService _screenshotService;
        private readonly IOpenCvService _templateMatcher;
        private readonly IAreaPointResolver _areaPointResolver;

        public ImageSearchStepWorker(
            IScreenshotService screenshotService,
            IOpenCvService templateMatcher,
            IAreaPointResolver areaPointResolver)
        {
            _screenshotService = screenshotService;
            _templateMatcher = templateMatcher;
            _areaPointResolver = areaPointResolver;
        }

        public async Task<ExecutionStep> ExecuteAsync(FlowStep step, IExecutionCacheService cache, CancellationToken ct)
        {
            if (step.FlowAreaId == null)
                return ExecutionStep.Failure("This step has no search area.");

            AreaResolution area = await _areaPointResolver.ResolveAreaAsync(step.FlowAreaId.Value, ct);
            if (!area.IsResolved)
                return ExecutionStep.Failure(area.Error);

            if (step.SearchMode == SearchModeEnum.WAIT_UNTIL_FOUND || step.SearchMode == SearchModeEnum.WAIT_UNTIL_NOT_FOUND)
                return await LoopSearchAsync(step, area.Bounds, cache, ct);

            return Search(step, area.Bounds, cache);
        }


        // ================================================================
        // Private methods
        // ================================================================

        private async Task<ExecutionStep> LoopSearchAsync(FlowStep step, Rectangle bounds, IExecutionCacheService cache, CancellationToken ct)
        {
            bool wantFound = step.SearchMode == SearchModeEnum.WAIT_UNTIL_FOUND;
            DateTime giveUpAt = DateTime.UtcNow.AddMilliseconds(step.TimeoutMilliseconds);

            while (true)
            {
                ct.ThrowIfCancellationRequested();

                ExecutionStep search = Search(step, bounds, cache);
                bool found = search.Outcome == StepOutcomeEnum.SUCCESS;

                if (found == wantFound)
                    return search;

                // A zero timeout waits for ever.
                if (step.TimeoutMilliseconds > 0 && DateTime.UtcNow >= giveUpAt)
                    return ExecutionStep.Failure(Detail(step, "gave up waiting"));

                await Task.Delay(step.PollIntervalMilliseconds, ct);
            }
        }

        private ExecutionStep Search(FlowStep step, Rectangle bounds, IExecutionCacheService cache)
        {
            if (bounds.Width <= 0 || bounds.Height <= 0)
                return ExecutionStep.Failure("The search area has no size. The window is probably minimised.");

            RawImage haystack = _screenshotService.CaptureRaw(bounds);

            // The screenshot that was actually matched against, not one taken a moment later that
            // could show something else. Whether it is worth keeping is the cache's business.
            cache.RecordScreenshot(haystack, step);

            List<Point> hits = new List<Point>();

            foreach (FlowStepImage image in step.FlowStepImages)
            {
                IReadOnlyList<TemplateMatch> matches = _templateMatcher.Match(new TemplateMatchRequest
                {
                    Haystack = haystack,
                    TemplateImage = image.TemplateImage ?? [],
                    Mode = image.TemplateMatchMode ?? step.TemplateMatchMode,
                    Threshold = image.Accuracy ?? step.Accuracy,
                    AllowMultiScale = image.AllowMultiScale,
                    ScaleTolerance = image.ScaleTolerance,
                    MaxMatches = step.MaxMatches,
                });

                foreach (TemplateMatch match in matches)
                {
                    int x = bounds.Left + match.X + (int)MathF.Round(image.ClickOffsetX * match.Scale);
                    int y = bounds.Top + match.Y + (int)MathF.Round(image.ClickOffsetY * match.Scale);

                    hits.Add(new Point(x, y));

                    if (step.SearchMode != SearchModeEnum.FIND_ALL)
                        break;
                }

                if (hits.Count > 0 && step.SearchMode != SearchModeEnum.FIND_ALL)
                    break;
            }

            if (hits.Count == 0)
                return ExecutionStep.Failure(Detail(step, "no template matched"));

            if (step.SearchMode != SearchModeEnum.FIND_ALL)
                return ExecutionStep.Success(hits[0]);

            // Every hit came from this one screenshot. The walk takes them one at a time from here,
            // and each gets its own execution step rather than the search running again.
            cache.RecordMatches(step.Id, hits);

            ExecutionStep found = ExecutionStep.Success(hits[0]);
            found.MatchIndex = 0;
            found.MatchCount = hits.Count;

            return found;
        }

        /// <summary>Says what was being looked for and how hard, which is what a failure turns on.</summary>
        private static string Detail(FlowStep step, string outcome)
        {
            string templates = string.Join(", ", step.FlowStepImages.Select(x => x.Name));
            if (templates.Length == 0)
                templates = "no templates";

            return $"{outcome} - {templates}, {step.SearchMode} at {step.Accuracy} accuracy";
        }
    }
}
