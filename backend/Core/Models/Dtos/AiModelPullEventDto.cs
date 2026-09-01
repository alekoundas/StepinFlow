namespace Core.Models.Dtos
{
    /// <summary>
    /// How a download is going. Sent over the broadcast pipe rather than returned, because pulling
    /// a model is minutes and gigabytes and a request that waited for it would simply time out.
    /// </summary>
    public class AiModelPullEventDto
    {
        public string Model { get; set; } = string.Empty;

        /// <summary>Ollama's own wording - "pulling manifest", "verifying sha256 digest".</summary>
        public string Status { get; set; } = string.Empty;

        public long Completed { get; set; }
        public long Total { get; set; }

        public bool IsDone { get; set; }
        public string Error { get; set; } = string.Empty;
    }
}
