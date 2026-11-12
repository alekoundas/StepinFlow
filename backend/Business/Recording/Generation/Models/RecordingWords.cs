namespace Business.Recording.Generation.Models
{
    /// <summary>
    /// What a model answered about a recording's draft: words for its searches, and for a QA test
    /// the stages and checks it proposes, with what only the tester can answer and what they should
    /// check. Read from JSON, so any of it may be missing.
    /// </summary>
    public sealed class RecordingWords
    {
        public List<StepWords>? Steps { get; set; }
        public List<StageWords>? Stages { get; set; }
        public List<CheckWords>? Checks { get; set; }
        public List<string?>? Questions { get; set; }
        public List<string?>? Notes { get; set; }
    }
}
