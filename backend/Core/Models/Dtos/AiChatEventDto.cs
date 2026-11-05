using Core.Enums;

namespace Core.Models.Dtos
{
    /// <summary>One thing that happened to an answer, sent as AI_CHAT_EVENT.</summary>
    public class AiChatEventDto
    {
        public string AnswerId { get; set; } = string.Empty;
        public AiChatEventTypeEnum Type { get; set; }

        /// <summary>STARTED: the model answering.</summary>
        public string Model { get; set; } = string.Empty;

        /// <summary>STARTED: the pictures that went with the question, by name.</summary>
        public List<string> Images { get; set; } = new List<string>();

        /// <summary>TOOL_STARTED and TOOL_FINISHED: which call this is.</summary>
        public string ActivityId { get; set; } = string.Empty;

        /// <summary>TOOL_STARTED: what it is doing, in plain words.</summary>
        public string Activity { get; set; } = string.Empty;

        /// <summary>TOOL_FINISHED: the steps it returned.</summary>
        public List<AiChatStepReferenceDto> Steps { get; set; } = new List<AiChatStepReferenceDto>();

        /// <summary>TEXT: what was added. DONE: the whole answer.</summary>
        public string Text { get; set; } = string.Empty;

        /// <summary>FAILED: what went wrong, then what it said about it.</summary>
        public string Error { get; set; } = string.Empty;
        public string ErrorDetail { get; set; } = string.Empty;
    }
}
