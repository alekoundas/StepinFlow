namespace Core.Models.Business
{
    /// <summary>
    /// What a search found, and how close it came when it found nothing.
    /// </summary>
    public sealed class TemplateMatchOutcome
    {
        /// <summary>Every match at or above the threshold, best first. Empty when nothing matched.</summary>
        public IReadOnlyList<TemplateMatchResult> Matches { get; set; } = [];

        /// <summary>
        /// The next candidates down, which did not clear the threshold, best first.
        ///
        /// Kept apart from <see cref="Matches"/> on purpose: anything that walks the matches acts
        /// on them, and a near miss put in that list would be clicked.
        ///
        /// The position is half the diagnosis. A 0.79 sitting on the button means the accuracy is
        /// a shade too tight; a 0.79 somewhere else means the template matches something it should
        /// not, and loosening the accuracy would make the flow click the wrong thing.
        /// </summary>
        public IReadOnlyList<TemplateMatchResult> Rejected { get; set; } = [];

        /// <summary>The best score in the frame, hit or not, which is what a run records.</summary>
        public float? BestScore =>
            Matches.Count > 0 ? Matches[0].Score :
            Rejected.Count > 0 ? Rejected[0].Score :
            null;
    }
}
