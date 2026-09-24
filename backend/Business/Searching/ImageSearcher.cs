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

            // Take screenshot.
            RawImage haystack = _screenshotService.CaptureRaw(bounds);
            if (haystack.IsEmpty)
                return new ImageSearchResult { Error = "The search area produced no pixels." };

            // Compare every template image.
            foreach (SearchTemplate template in templates)
            {
                TemplateMatchOutcome outcome = Match(haystack, bounds.Width, settings, template);
                outcomes.Add(outcome);

                // Whether it passed or not, so a run records how close a search came. Across every
                // template, because the closest one is the one worth reporting.
                bestScore = Best(bestScore, outcome.BestScore);

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

                // The template wins where it says anything, and says nothing by default.
                Mode = template.Mode ?? settings.Mode,
                AccuracyThreshold = template.Accuracy ?? settings.Accuracy,

                ScaleRatio = ScaleRatio(template.AuthoredFrameWidth, areaWidth),
                AllowMultiScale = template.AllowMultiScale,
                ScaleTolerance = template.ScaleTolerance,

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

        // The higher of two scores, either of which may be missing.
        private static float? Best(float? left, float? right)
        {
            if (left == null)
                return right;

            if (right == null)
                return left;

            return MathF.Max(left.Value, right.Value);
        }
    }
}
