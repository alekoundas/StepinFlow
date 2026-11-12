namespace Business.Recording.Generation.Models
{
    /// <summary>What a model wrote for one search of a recording's draft, by the id the outline gave it. Read from JSON, so nothing in it is trusted to be there.</summary>
    public sealed class StepWords
    {
        public string? Id { get; set; }
        public string? Name { get; set; }
        public string? Comment { get; set; }

        /// <summary>What its End Execution says when it is never found.</summary>
        public string? NotFound { get; set; }
    }
}
