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

        public ImageSearchResult Search(AreaResolution area, SearchSettings settings, IReadOnlyList<SearchTemplate> templates, bool stopAtFirstHit)
        {
            Rectangle bounds = area.Bounds;

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
                TemplateMatchOutcome outcome = Match(haystack, area, settings, template);
                if (outcome.Error != null)
                    return new ImageSearchResult { Error = $"Template {index + 1}: {outcome.Error}" };

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

        private TemplateMatchOutcome Match(RawImage haystack, AreaResolution area, SearchSettings settings, SearchTemplate template)
        {
            TemplateMatchRequest request = new TemplateMatchRequest
            {
                Haystack = haystack,
                TemplateImage = template.Image,

                // The mode is the step's, for every template; the accuracy is each template's own.
                Mode = settings.Mode,
                AccuracyThreshold = template.Accuracy,

                ScaleRatio = ScaleRatio(template, area),

                // Only FIND_ALL wants more than the first hit; everything else stops at one.
                MaxMatches = settings.SearchMode == SearchModeEnum.FIND_ALL ? settings.MaxMatches : 1,
            };

            TemplateMatchOutcome result = _templateMatcher.Match(request);

            return result;
        }

        // How much bigger or smaller the template is here than where it was captured, and the area
        // decides which question that is. A DPI area's contents keep their size when the window
        // changes - a browser reflows - so only the monitor moves them. An AREA area's contents fill
        // it, so they follow its size, by the smaller ratio: a game that changes shape letterboxes
        // rather than stretches. Anything not recorded leaves the template as it is.
        private static float ScaleRatio(SearchTemplate template, AreaResolution area)
        {
            if (area.ScalesWith == ScalesWithEnum.DPI)
            {
                if (template.AuthoredDpi <= 0 || area.Dpi <= 0)
                    return 1f;

                return (float)area.Dpi / template.AuthoredDpi;
            }

            bool hasWidth = template.AuthoredFlowAreaWidth > 0;
            bool hasHeight = template.AuthoredFlowAreaHeight > 0;
            float width = 0f;
            float height = 0f;

            if (hasWidth)
                width = (float)area.Bounds.Width / template.AuthoredFlowAreaWidth;

            if (hasHeight)
                height = (float)area.Bounds.Height / template.AuthoredFlowAreaHeight;

            if (hasWidth && hasHeight)
                return MathF.Min(width, height);

            if (hasWidth)
                return width;

            if (hasHeight)
                return height;

            return 1f;
        }
    }
}
