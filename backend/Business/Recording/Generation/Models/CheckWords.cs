namespace Business.Recording.Generation.Models
{
    /// <summary>A check a model proposed for a QA test: words that appeared after a search's click, which prove it worked.</summary>
    public sealed class CheckWords
    {
        public string? After { get; set; }
        public string? Words { get; set; }
        public string? Name { get; set; }
        public string? Comment { get; set; }
        public string? NotFound { get; set; }
    }
}
