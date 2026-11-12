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

        /// <summary>Which template the first hit is, and the scale it matched at.</summary>
        public int? FirstHitTemplateIndex { get; set; }
        public float FirstHitScale { get; set; } = 1f;

        /// <summary>
        /// Shape and colour: the best candidate whose shape passed and whose colour or contrast did
        /// not, across every template, and which template it was.
        /// </summary>
        public TemplateMatchResult? BestRefused { get; set; }
        public int? BestRefusedTemplateIndex { get; set; }
    }
}
