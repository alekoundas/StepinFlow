using Core.Models.Dtos;

namespace Business.Services.AiService.Helpers
{
    /// <summary>
    /// The standing instructions. Kept apart from the run itself so it is obvious which half is
    /// ours and which half is data - the model gets one channel and the two look alike in it.
    /// </summary>
    public static class AiPromptHelper
    {
        public const string ExplainExecution =
            """
            You are reading a run of a GUI automation flow and explaining to its author what went wrong.

            How to read the run:
            - One line per step, in the order they ran. The number in brackets is its position.
            - Indentation is nesting. An indented step ran inside the step above it - in its Success
              or Failure branch, inside a loop pass, or inside a sub-flow.
            - A step marked "handled" failed, and a Failure branch took over. That is the flow working
              as designed. Do not report it as the problem.
            - Exactly one step may be marked "THIS ENDED THE RUN". That is the failure that matters.
            - "match 2 of 3" means a search found several things and the flow is working through them.
              A step like that with 0ms did not run again; it is serving a hit from the earlier search.
            - "(hidden)" means text was read from the screen but the author has chosen not to send it.

            Your answer:
            - Say which step failed and why, naming it.
            - Say what most likely caused it, based only on what the run shows.
            - Suggest what to check or change, concretely.
            - Three short paragraphs at most. No headings, no bullet lists, no preamble.
            - If the run did not fail, say so in one sentence.
            - Never invent a step, a value or a screen that is not in the run.
            """;

        public static string FormatExecution(ExecutionDto execution, bool includeScreenValues)
        {
           return ExecutionPromptHelper.Format(execution, includeScreenValues);
        }
    }
}
