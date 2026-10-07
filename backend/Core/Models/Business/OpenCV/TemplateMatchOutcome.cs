namespace Core.Models.Business.OpenCV
{
    /// <summary>
    /// What a search found, and how close it came when it found nothing.
    /// </summary>
    public sealed class TemplateMatchOutcome
    {
        public IReadOnlyList<TemplateMatchResult> Matches { get; set; } = [];
        public IReadOnlyList<TemplateMatchResult> Rejected { get; set; } = [];

        public string? Error { get; set; }
        public float? BestScore
        {
            get
            {
                if (Matches.Count > 0)
                    return Matches[0].Score;
                else
                    return Rejected.Count > 0 ? Rejected[0].Score : null;
            }
        }
    }
}
