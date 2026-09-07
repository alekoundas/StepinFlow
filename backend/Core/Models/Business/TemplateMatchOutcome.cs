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

        public float? BestScore =>
            Matches.Count > 0 ? Matches[0].Score :
            Rejected.Count > 0 ? Rejected[0].Score :
            null;
    }
}
