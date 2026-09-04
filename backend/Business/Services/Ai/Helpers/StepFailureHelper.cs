using Core.Enums;

namespace Business.Services.Ai.Helpers
{
    /// <summary>
    /// What a failed step meant to the run it was in.
    ///
    /// Outcome says a step failed. It does not say whether that mattered, and the two cases look
    /// nothing alike: one failure ended the run, and every other one was caught by a Failure branch
    /// the author wrote on purpose. A run can read COMPLETED and still be full of the second kind.
    ///
    /// Both readers of a run need the same answer - the rendered text the explain page sends, and
    /// the rows the chat tools return - so neither works it out for itself.
    /// </summary>
    public static class StepFailureHelper
    {
        /// <summary>The one failure that stopped the run, when a run was stopped by one.</summary>
        public static bool EndedRun(int? flowStepId, int? errorFlowStepId)
        {
            return errorFlowStepId != null && flowStepId == errorFlowStepId;
        }

        /// <summary>Failed, and a Failure branch carried the flow on from there.</summary>
        public static bool WasHandled(StepOutcomeEnum outcome, int? flowStepId, int? errorFlowStepId)
        {
            return outcome == StepOutcomeEnum.FAILURE && !EndedRun(flowStepId, errorFlowStepId);
        }
    }
}
