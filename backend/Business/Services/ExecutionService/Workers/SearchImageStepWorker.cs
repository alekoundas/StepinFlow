using System.Drawing;
using System.Globalization;

using Business.Searching;
using Business.Services.AreaPointService;
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
    public class SearchImageStepWorker : IStepWorker
    {
        private readonly IImageSearcher _imageSearcher;
        private readonly IAreaPointResolver _areaPointResolver;
        private readonly TimeProvider _timeProvider;

        public SearchImageStepWorker(
            IImageSearcher imageSearcher,
            IAreaPointResolver areaPointResolver,
            TimeProvider timeProvider)
        {
            _imageSearcher = imageSearcher;
            _areaPointResolver = areaPointResolver;
            _timeProvider = timeProvider;
        }

        public async Task<ExecutionStep> ExecuteAsync(FlowStep step, IExecutionCacheService cache, CancellationToken ct)
        {
            if (step.FlowAreaId == null)
                return ExecutionStep.Failure("This step has no search area.");

            AreaResolution area = await _areaPointResolver.ResolveAreaAsync(step.FlowAreaId.Value, ct);
            if (!area.IsResolved)
                return ExecutionStep.Failure(area.Error);

            if (step.SearchMode == SearchModeEnum.WAIT_UNTIL_FOUND || step.SearchMode == SearchModeEnum.WAIT_UNTIL_NOT_FOUND)
                return await LoopSearchAsync(step, area, cache, ct);

            return Search(step, area, cache);
        }


        // ================================================================
        // Private methods
        // ================================================================

        private async Task<ExecutionStep> LoopSearchAsync(FlowStep step, AreaResolution area, IExecutionCacheService cache, CancellationToken ct)
        {
            bool wantFound = step.SearchMode == SearchModeEnum.WAIT_UNTIL_FOUND;
            DateTime giveUpAt = _timeProvider.GetUtcNow().UtcDateTime.AddMilliseconds(step.TimeoutMilliseconds);
            float? bestOverPolls = null;
            int? bestTemplateOverPolls = null;

            while (true)
            {
                ct.ThrowIfCancellationRequested();

                ExecutionStep search = Search(step, area, cache);
                bool found = search.Outcome == StepOutcomeEnum.SUCCESS;

                if (found == wantFound)
                    return search;

                // A zero timeout waits for ever.
                // The closest any attempt came, not the last one. "It peaked at 0.78 over sixty
                // tries" and "it never passed 0.40" want different fixes; the final poll says
                // neither.
                if (search.BestScore != null && (bestOverPolls == null || search.BestScore > bestOverPolls))
                {
                    bestOverPolls = search.BestScore;
                    bestTemplateOverPolls = search.BestTemplateId;
                }

                if (step.TimeoutMilliseconds > 0 && _timeProvider.GetUtcNow().UtcDateTime >= giveUpAt)
                {
                    ExecutionStep gaveUp = ExecutionStep.Failure(Detail(step, "gave up waiting"));
                    gaveUp.Screenshot = search.Screenshot;
                    gaveUp.BestScore = bestOverPolls;
                    gaveUp.BestTemplateId = bestTemplateOverPolls;

                    return gaveUp;
                }

                await Task.Delay(step.PollIntervalMilliseconds, ct);
            }
        }

        private ExecutionStep Search(FlowStep step, AreaResolution area, IExecutionCacheService cache)
        {
            bool findAll = step.SearchMode == SearchModeEnum.FIND_ALL;
            List<FlowStepTemplate> images = step.FlowStepTemplates.ToList();
            List<SearchTemplate> templates = images.Select(x => SearchTemplate.From(x)).ToList();

            ImageSearchResult search = _imageSearcher.Search(area, SearchSettings.From(step), templates, stopAtFirstHit: !findAll);
            if (search.Error != null)
                return ExecutionStep.Failure(search.Error);

            // The screenshot that was actually matched against, not one taken a moment later that
            // could show something else. Whether it is worth keeping is the cache's business.
            ExecutionScreenshot? screenshot = cache.EncodeForHistory(search.Haystack, step);

            ExecutionStep result = Result(step, area.Bounds, search, cache, findAll);
            result.Screenshot = screenshot;
            result.BestTemplateId = search.BestTemplateIndex is int best ? images[best].Id : null;

            return result;
        }

        // What was found, said as an execution step. The points arrive relative to the search
        // area, and a click needs them on the screen.
        private static ExecutionStep Result(FlowStep step, Rectangle bounds, ImageSearchResult search, IExecutionCacheService cache, bool findAll)
        {
            if (search.Hits.Count == 0)
            {
                ExecutionStep missed = ExecutionStep.Failure(Detail(step, "no template matched"));
                missed.BestScore = search.BestScore;

                return missed;
            }

            List<Point> hits = search.Hits.Select(x => new Point(bounds.Left + x.X, bounds.Top + x.Y)).ToList();

            if (!findAll)
            {
                ExecutionStep hit = ExecutionStep.Success(hits[0]);
                hit.BestScore = search.BestScore;

                return hit;
            }

            // Every hit came from this one screenshot. The walk takes them one at a time from here,
            // and each gets its own execution step rather than the search running again.
            cache.RecordMatches(step.Id, hits);

            ExecutionStep found = ExecutionStep.Success(hits[0]);
            found.MatchIndex = 0;
            found.MatchCount = hits.Count;
            found.BestScore = search.BestScore;

            return found;
        }

        /// <summary>Says what was being looked for and how hard, which is what a failure turns on.</summary>
        private static string Detail(FlowStep step, string outcome)
        {
            string templates = string.Join(", ", step.FlowStepTemplates.Select(x => $"{x.Name} at {x.Accuracy.ToString("0.00", CultureInfo.InvariantCulture)}"));
            if (templates.Length == 0)
                templates = "no templates";

            return $"{outcome} - {templates}, {step.SearchMode}";
        }
    }
}
