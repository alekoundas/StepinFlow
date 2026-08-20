using Core.Enums;

namespace Core.Models.Dtos
{
    /// <summary>
    /// Everything wrong with a flow, in one answer. The tree badges its rows from this and the
    /// execution page blocks Run from the same list, so the two can never disagree about whether
    /// a flow is runnable.
    /// </summary>
    public class FlowValidationResultDto
    {
        public bool HasErrors { get; set; }
        public List<FlowValidationIssueDto> Issues { get; set; } = new List<FlowValidationIssueDto>();
    }

    public class FlowValidationIssueDto
    {
        /// <summary>Null for a problem with the flow itself rather than one of its steps.</summary>
        public int? FlowStepId { get; set; }
        public string FlowStepName { get; set; } = string.Empty;

        public ValidationSeverityEnum Severity { get; set; }
        public FlowValidationCodeEnum Code { get; set; }
        public string Message { get; set; } = string.Empty;
    }
}
