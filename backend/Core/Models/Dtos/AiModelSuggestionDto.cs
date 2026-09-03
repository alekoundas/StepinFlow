namespace Core.Models.Dtos
{
    /// <summary>
    /// A model worth offering to somebody who has none. Ollama's library is far larger than this;
    /// the point of a short list is that a first choice is obvious rather than researched.
    /// </summary>
    public class AiModelSuggestionDto
    {
        public string Name { get; set; } = string.Empty;

        /// <summary>Roughly what will be downloaded. Approximate, and only there to set expectations.</summary>
        public string Size { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;

        /// <summary>Already downloaded, so the button says so rather than offering it again.</summary>
        public bool IsInstalled { get; set; }

        /// <summary>
        /// What it is expected to do, hand kept - Ollama only answers this for a model that is
        /// already pulled. Once it is installed the real capabilities come from the provider and
        /// these are not used.
        /// </summary>
        public IReadOnlyList<string> Capabilities { get; set; } = [];
    }
}
