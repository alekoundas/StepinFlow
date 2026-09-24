namespace Core.Models.Business
{
    /// <summary>
    /// What a search found, and how close it came when it found nothing.
    /// </summary>
    public sealed class TemplateMatchOutcome
    {
        public IReadOnlyList<TemplateMatchResult> Matches { get; set; } = [];

        /// <summary>
        /// The next candidates down, which did not clear the threshold, best first.
        /// </summary>
        public IReadOnlyList<TemplateMatchResult> Rejected { get; set; } = [];

        /// <summary>
        /// Set when the search could not be made at all - a template scaled larger than the
        /// screenshot, or down to nothing. Not "not found": the ratio is wrong, and a low score
        /// would hide that.
        /// </summary>
        public string? Error { get; set; }

        public float? BestScore =>
            Matches.Count > 0 ? Matches[0].Score :
            Rejected.Count > 0 ? Rejected[0].Score :
            null;
    }
}
