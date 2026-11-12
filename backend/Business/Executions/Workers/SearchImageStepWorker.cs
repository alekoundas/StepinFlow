using System.Drawing;
using System.Globalization;

using Business.Searching;
using Business.AreaPoint;
using Core.Enums;
using Core.Models.Business;
using Core.Models.Database;
using Core.Models.Business.AreaPointResolution;
using Core.Models.Business.OpenCV;

namespace Business.Executions.Workers
{
    /// <summary>
    /// Looks for the step's templates in its search area.
    ///
    /// One screenshot per attempt, and every template matched against it.
    /// FIND_ALL returns every hit from that one screenshot rather than searching again between them.
    /// </summary>
    public class SearchImageStepWorker : IStepWorker
    {
        private const int STILL_PAUSE_MILLISECONDS = 100;
        private const int STILL_TOLERANCE_PIXELS = 2;

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

            bool isWaiting = step.SearchMode == SearchModeEnum.WAIT_UNTIL_FOUND
                || step.SearchMode == SearchModeEnum.WAIT_UNTIL_STILL
                || step.SearchMode == SearchModeEnum.WAIT_UNTIL_NOT_FOUND;
            if (isWaiting)
                return await LoopSearchAsync(step, step.FlowAreaId.Value, cache, ct);

            AreaResolution area = await _areaPointResolver.ResolveAreaAsync(step.FlowAreaId.Value, ct);
            if (!area.IsResolved)
                return ExecutionStep.Failure(area.Error);

            ImageSearchResult search = Look(step, area);
            if (search.Error != null)
                return ExecutionStep.Failure(search.Error);

            return ToExecutionStep(step, area, search, cache);
        }


        // ================================================================
        // Private methods
        // ================================================================

        // The area is found again on every try: the window may open, move or change size while this
        // waits. A try that cannot look - no window yet, a minimised one, a template bigger than the
        // window is now - is neither found nor gone, and the next one may.
        //
        // Waiting until still, a find is looked at again a moment later and counts only in the same
        // place. One that moved is held to where it went at the next poll.
        private async Task<ExecutionStep> LoopSearchAsync(FlowStep step, int flowAreaId, IExecutionCacheService cache, CancellationToken ct)
        {
            bool wantFound = step.SearchMode != SearchModeEnum.WAIT_UNTIL_NOT_FOUND;
            bool waitsUntilStill = step.SearchMode == SearchModeEnum.WAIT_UNTIL_STILL;
            DateTime giveUpAt = _timeProvider.GetUtcNow().UtcDateTime.AddMilliseconds(step.TimeoutMilliseconds);
            float? bestOverPolls = null;
            int? bestTemplateOverPolls = null;
            ExecutionScreenshot? lastScreenshot = null;
            Sighting? lastSighting = null;
            string? stillMoving = null;
            string? lastRefusal = null;

            while (true)
            {
                ct.ThrowIfCancellationRequested();

                AreaResolution area = await _areaPointResolver.ResolveAreaAsync(flowAreaId, ct);
                if (area.Unresolved == AreaUnresolvedEnum.NOT_SET_UP)
                    return ExecutionStep.Failure(area.Error);

                // What it waits to see go was in that window, so a window that has closed took it.
                if (!wantFound && area.Unresolved == AreaUnresolvedEnum.NOT_ON_SCREEN)
                    return ExecutionStep.Success(message: $"No longer on screen: {area.Error}");

                ExecutionStep? search = null;
                ImageSearchResult? look = null;
                string? cannotLook = area.Error;
                if (area.IsResolved)
                {
                    look = Look(step, area);
                    cannotLook = look.Error;

                    if (look.Error == null)
                        search = ToExecutionStep(step, area, look, cache);
                }

                Sighting? sighting = null;

                if (search != null && look != null)
                {
                    bool found = search.Outcome == StepOutcomeEnum.SUCCESS;
                    if (found && wantFound)
                    {
                        if (!waitsUntilStill)
                            return search;

                        sighting = new Sighting(search.Location!.Value, look.FirstHitTemplateIndex, look.FirstHitScale);
                        if (lastSighting != null)
                        {
                            stillMoving = HowItMoved(lastSighting, sighting);
                            if (stillMoving == null)
                                return search;
                        }
                    }

                    lastRefusal = Refusal(step, look);

                    // Gone is what this mode waits for, so it is a success - the search itself said
                    // "nothing matched", which on its own reads as a failure.
                    if (!found && !wantFound)
                    {
                        string goneMessage = "No longer on screen.";
                        if (lastRefusal != null)
                            goneMessage = $"No longer on screen as it was: {lastRefusal}.";

                        ExecutionStep gone = ExecutionStep.Success(message: goneMessage);
                        gone.Screenshot = search.Screenshot;
                        gone.BestScore = search.BestScore;
                        gone.BestTemplateId = search.BestTemplateId;

                        return gone;
                    }

                    // The closest any attempt came, not the last one. "It peaked at 0.78 over sixty
                    // tries" and "it never passed 0.40" want different fixes; the final poll says
                    // neither.
                    if (search.BestScore != null && (bestOverPolls == null || search.BestScore > bestOverPolls))
                    {
                        bestOverPolls = search.BestScore;
                        bestTemplateOverPolls = search.BestTemplateId;
                    }

                    lastScreenshot = search.Screenshot;
                }

                // Found in time, so the look that tells whether it was still is taken whatever the time.
                bool isFirstSighting = sighting != null && lastSighting == null;
                lastSighting = sighting;

                // A zero timeout waits for ever.
                if (!isFirstSighting && step.TimeoutMilliseconds > 0 && _timeProvider.GetUtcNow().UtcDateTime >= giveUpAt)
                {
                    string message = Detail(step, "gave up waiting", bestOverPolls, bestTemplateOverPolls, lastRefusal);
                    if (lastSighting != null)
                        message = Detail(step, $"found but still moving when the time ran out, {stillMoving}", null, null, null);
                    else if (cannotLook != null)
                        message += $". The last try could not look: {cannotLook}";

                    ExecutionStep gaveUp = ExecutionStep.Failure(message);
                    gaveUp.Screenshot = lastScreenshot;
                    gaveUp.BestScore = bestOverPolls;
                    gaveUp.BestTemplateId = bestTemplateOverPolls;

                    return gaveUp;
                }

                int pause = step.PollIntervalMilliseconds;
                if (isFirstSighting)
                    pause = STILL_PAUSE_MILLISECONDS;

                await Task.Delay(pause, ct);
            }
        }

        // Null when the second sighting is the first one standing still: the same template, within
        // a pixel or two of jitter, grown with the scale the template was matched at.
        private static string? HowItMoved(Sighting before, Sighting now)
        {
            if (before.TemplateIndex != now.TemplateIndex)
                return "a different template matched each time";

            int moved = Math.Max(Math.Abs(now.Location.X - before.Location.X), Math.Abs(now.Location.Y - before.Location.Y));
            int tolerance = Math.Max(STILL_TOLERANCE_PIXELS, (int)MathF.Round(STILL_TOLERANCE_PIXELS * now.Scale));
            if (moved <= tolerance)
                return null;

            return $"last moved {moved} px";
        }

        private ImageSearchResult Look(FlowStep step, AreaResolution area)
        {
            bool findAll = step.SearchMode == SearchModeEnum.FIND_ALL;
            List<SearchTemplate> templates = step.FlowStepTemplates.Select(x => SearchTemplate.From(x)).ToList();

            return _imageSearcher.Search(area, SearchSettings.From(step), templates, stopAtFirstHit: !findAll);
        }

        private static ExecutionStep ToExecutionStep(FlowStep step, AreaResolution area, ImageSearchResult search, IExecutionCacheService cache)
        {
            bool findAll = step.SearchMode == SearchModeEnum.FIND_ALL;
            List<FlowStepTemplate> images = step.FlowStepTemplates.ToList();

            // The screenshot that was actually matched against, not one taken a moment later that
            // could show something else. Whether it is worth keeping is the cache's business.
            ExecutionScreenshot? screenshot = cache.EncodeForHistory(search.Haystack, step);

            int? bestTemplateId = null;
            if (search.BestTemplateIndex is int best)
                bestTemplateId = images[best].Id;

            ExecutionStep result = Result(step, area.Bounds, search, bestTemplateId, cache, findAll);
            result.Screenshot = screenshot;
            result.BestTemplateId = bestTemplateId;

            return result;
        }

        // What was found, said as an execution step. The points arrive relative to the search
        // area, and a click needs them on the screen.
        private static ExecutionStep Result(FlowStep step, Rectangle bounds, ImageSearchResult search, int? bestTemplateId, IExecutionCacheService cache, bool findAll)
        {
            if (search.Hits.Count == 0)
            {
                ExecutionStep missed = ExecutionStep.Failure(Detail(step, "no template matched", search.BestScore, bestTemplateId, Refusal(step, search)));
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

        // What was looked for and how hard, and which template came closest: each has its own
        // accuracy, so 0.78 means nothing until it is read against the bar of the one that scored it.
        private static string Detail(FlowStep step, string outcome, float? bestScore, int? bestTemplateId, string? refusal)
        {
            string templates = string.Join(", ", step.FlowStepTemplates.Select(x => $"{x.FlowTemplate.Name} at {Score(x.Accuracy)}"));
            if (templates.Length == 0)
                templates = "no templates";

            string found = outcome;

            FlowStepTemplate? closest = step.FlowStepTemplates.FirstOrDefault(x => x.Id == bestTemplateId);
            if (bestScore != null && closest != null)
                found += $", closest {closest.FlowTemplate.Name} at {Score(bestScore.Value)}";

            if (refusal != null)
                found += $", {refusal}";

            return $"{found} - {templates}, {step.SearchMode}";
        }

        // A template whose shape was on screen and whose colour or contrast was not: the thing, most
        // likely, in another state. Lowering its accuracy would not find it.
        private static string? Refusal(FlowStep step, ImageSearchResult search)
        {
            TemplateMatchResult? refused = search.BestRefused;
            if (refused == null || search.BestRefusedTemplateIndex is not int index)
                return null;

            string name = step.FlowStepTemplates.ElementAt(index).FlowTemplate.Name;

            if (refused.RefusedBy == MatchRefusalEnum.CONTRAST)
                return $"{name} at {Score(refused.Score)} refused by its contrast, {Score(refused.Contrast ?? 0f)} where {Score(ColourCheckLimits.CONTRAST_MIN)} to {Score(ColourCheckLimits.CONTRAST_MAX)} is allowed";

            return $"{name} at {Score(refused.Score)} refused by its colour, {Whole(refused.Colour ?? 0f)} apart where {Whole(refused.ColourLimit ?? 0f)} is allowed";
        }

        private static string Score(float value)
        {
            return value.ToString("0.00", CultureInfo.InvariantCulture);
        }

        private static string Whole(float value)
        {
            return value.ToString("0", CultureInfo.InvariantCulture);
        }

        // Where a wait found its template on screen, which template, and at what scale.
        private sealed record Sighting(Point Location, int? TemplateIndex, float Scale);
    }
}
