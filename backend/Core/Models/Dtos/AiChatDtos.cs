namespace Core.Models.Dtos
{
    /// <summary>
    /// One turn. The page keeps the whole conversation and sends it back every time, so nothing
    /// here has to remember a session or expire one.
    /// </summary>
    public class AiChatMessageDto
    {
        /// <summary>"user" or "assistant".</summary>
        public string Role { get; set; } = string.Empty;
        public string Text { get; set; } = string.Empty;
    }

    public class AiChatRequestDto
    {
        public List<AiChatMessageDto> Messages { get; set; } = new List<AiChatMessageDto>();

        /// <summary>
        /// The execution this conversation was started from, when it was started from one. It is
        /// what lets the assistant be shown the screenshots it kept.
        /// </summary>
        public int? ExecutionId { get; set; }
    }

    public class AiChatAnswerDto
    {
        public string Answer { get; set; } = string.Empty;

        /// <summary>What it looked at to answer, so the reply can be checked rather than trusted.</summary>
        public List<string> ToolCalls { get; set; } = new List<string>();

        /// <summary>
        /// The pictures it was handed. Kept apart from ToolCalls because it did not ask for these -
        /// they arrive with the question - and because an answer that ignores a screenshot it was
        /// given reads very differently once you know it had one.
        /// </summary>
        public List<string> Images { get; set; } = new List<string>();

        public string Error { get; set; } = string.Empty;
    }

    /// <summary>Why the feature is or is not offered, in the terms the panel explains it in.</summary>
    public class AiChatAvailabilityDto
    {
        public bool IsAvailable { get; set; }
        public string Model { get; set; } = string.Empty;
        public string Reason { get; set; } = string.Empty;
    }
}
