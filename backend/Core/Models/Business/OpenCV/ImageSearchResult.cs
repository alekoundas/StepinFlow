using System.Drawing;

using Core.Models.Business;
using Core.Models.Business.OpenCV;

namespace Business.Searching
{
    public sealed record ImageSearchResult
    {
        /// <summary> The screenshot that was matched against. </summary>
        public RawImage Haystack { get; set; } = new RawImage();
        public float? BestScore { get; set; }
        public int? BestTemplateIndex { get; set; }
        public string? Error { get; set; }

        /// <summary> One per template, in the order they were given.</summary>
        public IReadOnlyList<TemplateMatchOutcome> Outcomes { get; set; } = [];

        /// <summary>Where a click would land, relative to the search area.</summary>
        public IReadOnlyList<Point> Hits { get; set; } = [];

        /// <summary> Required templates that were not found. </summary>
        public IReadOnlyList<int> MissingRequired { get; set; } = [];

    }
}
