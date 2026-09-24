using System.Drawing;

using Core.Enums;
using Core.Models.Business;
using Core.Ports;

namespace Business.Searching
{
    /// <summary>
    /// Take a screenshot on the bounds and look for the templates in it.
    /// </summary>
    public sealed class ImageSearcher : IImageSearcher
    {
        private readonly IScreenshotService _screenshotService;
        private readonly IOpenCvService _templateMatcher;

        public ImageSearcher(IScreenshotService screenshotService, IOpenCvService templateMatcher)
        {
            _screenshotService = screenshotService;
            _templateMatcher = templateMatcher;
        }

        public ImageSearchResult Search(Rectangle bounds, SearchSettings settings, IReadOnlyList<SearchTemplate> templates, bool stopAtFirstHit)
        {
            if (bounds.Width <= 0 || bounds.Height <= 0)
                return new ImageSearchResult { Error = "The search area has no size. The window is probably minimised." };

            List<TemplateMatchOutcome> outcomes = new List<TemplateMatchOutcome>();
            List<Point> hits = new List<Point>();
            float? bestScore = null;
            int? bestTemplateIndex = null;

            // Take screenshot.
            RawImage haystack = _screenshotService.CaptureRaw(bounds);
            if (haystack.IsEmpty)
                return new ImageSearchResult { Error = "The search area produced no pixels." };

            // Compare every template image.
            for (int index = 0; index < templates.Count; index++)
            {
                SearchTemplate template = templates[index];
                TemplateMatchOutcome outcome = Match(haystack, bounds.Width, settings, template);
                outcomes.Add(outcome);

                // Whether it passed or not, so a run records how close a search came. Across every
                // template, because the closest one is the one worth reporting - with which one it
                // was, since each is measured against its own accuracy.
                if (outcome.BestScore != null && (bestScore == null || outcome.BestScore > bestScore))
                {
                    bestScore = outcome.BestScore;
                    bestTemplateIndex = index;
                }

                foreach (TemplateMatchResult match in outcome.Matches)
                {
                    hits.Add(template.ClickPoint(match));

                    if (stopAtFirstHit)
                        break;
                }

                if (hits.Count > 0 && stopAtFirstHit)
                    break;
            }

            return new ImageSearchResult
            {
                Haystack = haystack,
                Outcomes = outcomes,
                Hits = hits,
                BestScore = bestScore,
                BestTemplateIndex = bestTemplateIndex,
            };
        }


        // ================================================================
        // Private methods
        // ================================================================

        private TemplateMatchOutcome Match(RawImage haystack, int areaWidth, SearchSettings settings, SearchTemplate template)
        {
            TemplateMatchRequest request = new TemplateMatchRequest
            {
                Haystack = haystack,
                TemplateImage = template.Image,

                // The mode is the step's, for every template; the accuracy is each template's own.
                Mode = settings.Mode,
                AccuracyThreshold = template.Accuracy,

                ScaleRatio = ScaleRatio(template.AuthoredFlowAreaWidth, areaWidth),

                // Only FIND_ALL wants more than the first hit; everything else stops at one.
                MaxMatches = settings.SearchMode == SearchModeEnum.FIND_ALL ? settings.MaxMatches : 1,
            };

            TemplateMatchOutcome result = _templateMatcher.Match(request);

            return result;
        }

        // The area is a different size now than when the template was captured, so the template
        // is too. Zero on either side means nobody recorded it, and 1 leaves the template alone.
        private static float ScaleRatio(int authoredFrameWidth, int currentFrameWidth)
        {
            if (authoredFrameWidth <= 0 || currentFrameWidth <= 0)
                return 1f;

            return (float)currentFrameWidth / authoredFrameWidth;
        }
    }
}
