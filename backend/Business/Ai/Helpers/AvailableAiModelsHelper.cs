using Core.Models.Dtos;

namespace Business.Ai.Helpers
{
    /// <summary>
    /// A handful of local models worth suggesting to somebody who has none, smallest first. Each calls
    /// tools and sees images, which every AI feature needs.
    ///
    /// Hand kept, since Ollama only answers for a model already pulled; sizes from Ollama's library,
    /// 2026-11-13. Every request turns thinking off, and Qwen3-VL's plain tags are its thinking
    /// models, so its instruct one is offered.
    /// </summary>
    public static class AvailableAiModelsHelper
    {
        public static IReadOnlyList<AiModelSuggestionDto> Suggestions { get; } =
        [
            new AiModelSuggestionDto
            {
                Name = "qwen3.5:4b",
                Size = "about 3.3 GB",
                Description = "The small one, and the one StepinFlow is tested with. Start here.",
                Capabilities = ["tools", "vision"],
            },
            new AiModelSuggestionDto
            {
                Name = "qwen3-vl:4b-instruct",
                Size = "about 3.3 GB",
                Description = "Just as small, and tuned for reading screens and interfaces. Try it if qwen3.5:4b misreads your application.",
                Capabilities = ["tools", "vision"],
            },
            new AiModelSuggestionDto
            {
                Name = "qwen3.5:9b",
                Size = "about 6.6 GB",
                Description = "Qwen 3.5 again, bigger: a good balance once your graphics card has the memory for it.",
                Capabilities = ["tools", "vision"],
            },
            new AiModelSuggestionDto
            {
                Name = "gemma4:12b",
                Size = "about 7.7 GB",
                Description = "Google's, between the small ones and the big ones.",
                Capabilities = ["tools", "vision"],
            },
            new AiModelSuggestionDto
            {
                Name = "ministral-3:14b",
                Size = "about 9.1 GB",
                Description = "Mistral's, made to run on ordinary hardware.",
                Capabilities = ["tools", "vision"],
            },
            new AiModelSuggestionDto
            {
                Name = "mistral-small3.2:24b",
                Size = "about 15 GB",
                Description = "Big, and careful at following instructions and calling tools.",
                Capabilities = ["tools", "vision"],
            },
            new AiModelSuggestionDto
            {
                Name = "qwen3.5:27b",
                Size = "about 17 GB",
                Description = "The biggest here, for a strong graphics card. The bigger the model, the better it answers.",
                Capabilities = ["tools", "vision"],
            },
        ];
    }
}
