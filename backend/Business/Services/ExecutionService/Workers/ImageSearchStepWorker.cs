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
            float? bestOverPolls = null;

            while (true)
            {
                ct.ThrowIfCancellationRequested();

                ExecutionStep search = Search(step, bounds, cache);
                bool found = search.Outcome == StepOutcomeEnum.SUCCESS;

                if (found == wantFound)
                    return search;

                // A zero timeout waits for ever.
                // The closest any attempt came, not the last one. "It peaked at 0.78 over sixty
                // tries" and "it never passed 0.40" want different fixes; the final poll says
                // neither.
                bestOverPolls = Best(bestOverPolls, search.BestScore);

                if (step.TimeoutMilliseconds > 0 && DateTime.UtcNow >= giveUpAt)
                {
                    ExecutionStep gaveUp = ExecutionStep.Failure(Detail(step, "gave up waiting"));
                    gaveUp.Screenshot = search.Screenshot;
                    gaveUp.BestScore = bestOverPolls;

                    return gaveUp;
                }

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
            ExecutionScreenshot? screenshot = cache.RecordScreenshot(haystack, step);

            ExecutionStep result = Match(step, bounds, haystack, cache);
            result.Screenshot = screenshot;

            return result;
        }

        private ExecutionStep Match(FlowStep step, Rectangle bounds, RawImage haystack, IExecutionCacheService cache)
        {
            List<Point> hits = new List<Point>();
            float? bestScore = null;

            foreach (FlowStepImage image in step.FlowStepImages)
            {
                TemplateMatchOutcome outcome = _templateMatcher.Match(new TemplateMatchRequest
                {
                    Haystack = haystack,
                    TemplateImage = image.TemplateImage ?? [],
                    Mode = image.TemplateMatchMode ?? step.TemplateMatchMode,
                    Threshold = image.Accuracy ?? step.Accuracy,
                    ScaleRatio = ScaleRatio(image.AuthoredFrameWidth, bounds.Width),
                    AllowMultiScale = image.AllowMultiScale,
                    ScaleTolerance = image.ScaleTolerance,
                    MaxMatches = step.SearchMode == SearchModeEnum.FIND_ALL ? step.MaxMatches : 1,
                });

                IReadOnlyList<TemplateMatchResult> matches = outcome.Matches;

                // Whether it passed or not, so a run records how close a search came. Across every
                // template, because the closest one is the one worth reporting.
                bestScore = Best(bestScore, outcome.BestScore);

                foreach (TemplateMatchResult match in matches)
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
            {
                ExecutionStep missed = ExecutionStep.Failure(Detail(step, "no template matched"));
                missed.BestScore = bestScore;

                return missed;
            }

            if (step.SearchMode != SearchModeEnum.FIND_ALL)
            {
                ExecutionStep hit = ExecutionStep.Success(hits[0]);
                hit.BestScore = bestScore;

                return hit;
            }

            // Every hit came from this one screenshot. The walk takes them one at a time from here,
            // and each gets its own execution step rather than the search running again.
            cache.RecordMatches(step.Id, hits);

            ExecutionStep found = ExecutionStep.Success(hits[0]);
            found.MatchIndex = 0;
            found.MatchCount = hits.Count;
            found.BestScore = bestScore;

            return found;
        }

        /// <summary>The higher of two scores, either of which may be missing.</summary>
        private static float? Best(float? left, float? right)
        {
            if (left == null)
                return right;

            if (right == null)
                return left;

            return MathF.Max(left.Value, right.Value);
        }

        private static float ScaleRatio(int authoredFrameWidth, int currentFrameWidth)
        {
            if (authoredFrameWidth <= 0 || currentFrameWidth <= 0)
                return 1f;

            return (float)currentFrameWidth / authoredFrameWidth;
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
