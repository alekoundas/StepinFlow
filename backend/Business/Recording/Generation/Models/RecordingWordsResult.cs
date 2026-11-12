namespace Business.Recording.Generation.Models
{
    /// <summary>What asking the model gave: its words, or why there are none.</summary>
    public sealed class RecordingWordsResult
    {
        /// <summary>Null when no answer read as the JSON asked for, or the model could not be reached.</summary>
        public RecordingWords? Words { get; set; }

        public string? ErrorMessage { get; set; }

        public int Attempts { get; set; }

        /// <summary>The message each request was sent, exactly. The standing instructions are not in it.</summary>
        public string Request { get; set; } = string.Empty;
    }
}
