using System.Drawing;

using Core.Models.Business;

namespace Business.Searching
{
    /// <summary>
    /// What one look at the screen found.
    ///
    /// Everything here is about pixels: no execution step, no cache, no dto. The engine turns it
    /// into a result and the editor turns it into a report, and neither of those is a search.
    /// </summary>
    public sealed record ImageSearchResult
    {
        /// <summary>
        /// The screenshot that was matched against, not one taken a moment later that could show
        /// something else. The caller keeps it or encodes it; the search is done with it.
        /// </summary>
        public RawImage Haystack { get; init; } = new RawImage();

        /// <summary>
        /// One per template, in the order they were given - and a prefix of them when the search
        /// was told to stop at the first hit and did.
        /// </summary>
        public IReadOnlyList<TemplateMatchOutcome> Outcomes { get; init; } = [];

        /// <summary>Where a click would land, relative to the search area.</summary>
        public IReadOnlyList<Point> Hits { get; init; } = [];

        /// <summary>
        /// The closest anything came across every template, whether it passed or not. "It peaked
        /// at 0.78" and "it never passed 0.40" want different fixes.
        /// </summary>
        public float? BestScore { get; init; }

        /// <summary>Which of the templates given produced <see cref="BestScore"/>, by position.</summary>
        public int? BestTemplateIndex { get; init; }

        /// <summary>
        /// Set when the search could not be made: nothing to search, or a template that cannot fit
        /// at the ratio it scales to.
        /// </summary>
        public string? Error { get; init; }
    }
}
