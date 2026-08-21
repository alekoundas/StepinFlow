namespace Core.Models.Dtos
{
    /// <summary>
    /// Everything the extraction needs about where the step sits, so the placeholder can take
    /// exactly its position.
    /// </summary>
    public class ExtractSubFlowDto
    {
        public int FlowStepId { get; set; }
        public string Name { get; set; } = string.Empty;

        // Where the extracted step was, which is where the SUB_FLOW step goes.
        public int SourceRootId { get; set; }
        public int? SourceFlowId { get; set; }
        public int? SourceParentFlowStepId { get; set; }
        public int SourceOrderNumber { get; set; }
    }

    public class ExtractSubFlowResultDto
    {
        public int SubFlowId { get; set; }

        /// <summary>The SUB_FLOW step left in its place, to select once the tree reloads.</summary>
        public int FlowStepId { get; set; }

        public int MovedCount { get; set; }
    }
}
