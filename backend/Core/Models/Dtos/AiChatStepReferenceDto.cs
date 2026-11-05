namespace Core.Models.Dtos
{
    /// <summary>A step a tool returned, under the name the model read, so the answer can open it.</summary>
    public class AiChatStepReferenceDto
    {
        public int FlowStepId { get; set; }

        /// <summary>The flow it belongs to. A sub-flow's step opens the sub-flow.</summary>
        public int FlowId { get; set; }

        public string Name { get; set; } = string.Empty;
    }
}
