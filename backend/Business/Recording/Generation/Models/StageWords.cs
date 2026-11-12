namespace Business.Recording.Generation.Models
{
    /// <summary>A stage a model proposed for a QA test: its name, before which search, and what it proves.</summary>
    public sealed class StageWords
    {
        public string? Before { get; set; }
        public string? Name { get; set; }
        public string? Comment { get; set; }
    }
}
