namespace Core.Models.Dtos
{
    public class FlowStepMovePreviewDto
    {
        public bool IsValid { get; set; }
        public string? ErrorMessage { get; set; }

        public string MovedStepName { get; set; } = string.Empty;
        public string TargetParentName { get; set; } = string.Empty;

        /// <summary>How many steps travel with the moved one.</summary>
        public int MovedDescendantCount { get; set; }

        /// <summary>True when the step only changes position among its current siblings.</summary>
        public bool IsReorderOnly { get; set; }

        /// <summary>
        /// Steps whose FlowStepReference would stop resolving because the referenced step is no
        /// longer one of their ancestors after the move.
        /// </summary>
        public List<FlowStepBrokenReferenceDto> BrokenReferences { get; set; } = new List<FlowStepBrokenReferenceDto>();
    }

    public class FlowStepBrokenReferenceDto
    {
        public int FlowStepId { get; set; }
        public string FlowStepName { get; set; } = string.Empty;
        public string ReferencedStepName { get; set; } = string.Empty;
        public bool IsEndReference { get; set; }
    }
}
