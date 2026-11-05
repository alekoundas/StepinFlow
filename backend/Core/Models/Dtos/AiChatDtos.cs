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
        /// <summary>Chosen by the page. Every event about this answer carries it, and Stop names it.</summary>
        public string AnswerId { get; set; } = string.Empty;

        public List<AiChatMessageDto> Messages { get; set; } = new List<AiChatMessageDto>();

        /// <summary>What the user is looking at, so "this" means something. An execution says its own flow.</summary>
        public int? FlowId { get; set; }
        public int? ExecutionId { get; set; }
        public int? FlowStepId { get; set; }

        /// <summary>Show the model the pictures ExecutionId kept. They go with this question only.</summary>
        public bool SendScreenshots { get; set; }
    }

    /// <summary>Why the feature is or is not offered, in the terms the panel explains it in.</summary>
    public class AiChatAvailabilityDto
    {
        public bool IsAvailable { get; set; }
        public string Model { get; set; } = string.Empty;
        public string Reason { get; set; } = string.Empty;
        public bool IsCloud { get; set; }
        public bool CanSendScreenData { get; set; }
    }
}
