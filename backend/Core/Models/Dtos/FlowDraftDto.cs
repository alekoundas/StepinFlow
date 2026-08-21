using Core.Enums;

namespace Core.Models.Dtos
{
    /// <summary>
    /// A batch of proposed steps and where they should land. Produced by the recorder today and
    /// by the AI later; the wizard and the batch save only ever see this, so neither of them
    /// knows or cares which one made it.
    /// </summary>
    public class FlowDraftDto
    {
        public FlowDraftTargetDto Target { get; set; } = new FlowDraftTargetDto();
        public List<DraftStepDto> Steps { get; set; } = new List<DraftStepDto>();
    }

    /// <summary>
    /// Where the batch lands, in the same terms a drag and drop uses, plus the one thing a move
    /// never needs: a flow that does not exist yet.
    /// </summary>
    public class FlowDraftTargetDto
    {
        public int? TargetFlowId { get; set; }
        public int? TargetParentFlowStepId { get; set; }
        public int TargetIndex { get; set; }
    }

    public class DraftStepDto
    {
        /// <summary>
        /// Identity within the draft only. Steps refer to each other by this until the save
        /// swaps in the real ids it just generated.
        /// </summary>
        public int TempId { get; set; }

        public int? ParentTempId { get; set; }

        /// <summary>SUCCESS or FAILURE when the step sits in a branch of its parent.</summary>
        public FlowStepTypeEnum? ParentBranch { get; set; }

        /// <summary>
        /// Another step in this same draft whose result this one reads, by temp id. A click that
        /// acts on an image search cannot know the real id until the save has written both.
        /// </summary>
        public int? ReferenceTempId { get; set; }

        /// <summary>The step itself. Ids are unset; the save fills them in.</summary>
        public FlowStepDto Values { get; set; } = new FlowStepDto();

        /// <summary>
        /// A point to create on the flow and link to this step. A recorded click knows its
        /// coordinates but a cursor step reads its position from a FlowPoint, so one has to exist
        /// for the step to mean anything.
        /// </summary>
        public DraftPointDto? NewPoint { get; set; }

        /// <summary>The drop point of a recorded drag.</summary>
        public DraftPointDto? NewPointEnd { get; set; }

        /// <summary>What is still missing, from the same validator the tree badges use.</summary>
        public List<DraftIssueDto> Unresolved { get; set; } = new List<DraftIssueDto>();

        public DraftStepSourceEnum Source { get; set; }
        public DraftEvidenceDto? Evidence { get; set; }
    }

    public class DraftPointDto
    {
        public string Name { get; set; } = string.Empty;
        public int LocationX { get; set; }
        public int LocationY { get; set; }
    }

    /// <summary>Why this step was proposed, in whatever terms its source can offer.</summary>
    public class DraftEvidenceDto
    {
        /// <summary>Key into the recording session's screenshot store.</summary>
        public int? ScreenshotIndex { get; set; }

        public string? WindowTitle { get; set; }

        /// <summary>One line the wizard shows above the step.</summary>
        public string Summary { get; set; } = string.Empty;
    }

    public class DraftIssueDto
    {
        public FlowValidationCodeEnum Code { get; set; }
        public ValidationSeverityEnum Severity { get; set; }
        public string Message { get; set; } = string.Empty;
    }
}
