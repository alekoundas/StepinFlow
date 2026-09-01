using Core.Models.Dtos;

namespace Business.Services.Ai.Helpers
{
    /// <summary>
    /// A handful of local models worth suggesting to somebody who has none.
    ///
    /// Ollama's library runs to hundreds, and it has no listing api worth relying on, so this is a
    /// short opinionated list rather than a mirror of it. Anything not here can still be typed in -
    /// the point is that a first choice is obvious rather than researched.
    ///
    /// Sizes are approximate, and what actually arrives is whatever Ollama is serving that day.
    /// </summary>
    public static class AvailableAiModelsHelper
    {
        public static IReadOnlyList<AiModelSuggestionDto> Suggestions { get; } =
        [
            new AiModelSuggestionDto
            {
                Name = "qwen2.5:3b",
                Size = "about 2 GB",
                Description = "The small one. Runs on anything, and is enough to read a failed run.",
            },
            new AiModelSuggestionDto
            {
                Name = "qwen2.5:7b",
                Size = "about 5 GB",
                Description = "A good balance. Start here if you have the disk and the memory.",
            },
            new AiModelSuggestionDto
            {
                Name = "llama3.1:8b",
                Size = "about 5 GB",
                Description = "Comparable to Qwen 7B. Worth trying if the other one disappoints.",
            },
            new AiModelSuggestionDto
            {
                Name = "gemma2:9b",
                Size = "about 5.5 GB",
                Description = "Google's. Reads and summarises well.",
            },
            new AiModelSuggestionDto
            {
                Name = "phi4",
                Size = "about 9 GB",
                Description = "The biggest here, and the best at following instructions closely.",
            },
        ];
    }
}
