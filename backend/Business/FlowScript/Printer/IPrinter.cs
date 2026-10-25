using Business.FlowScript.Models;
using Core.Models.Database;

namespace Business.FlowScript.Text
{
    public interface IPrinter
    {
        /// <summary>
        /// A flow as the text that goes in a repository. Deterministic: the same flow writes the
        /// same bytes, which is what makes a round trip testable.
        /// </summary>
        string Write(FlowScriptSchema schema);

        /// <summary>
        /// One step as a flow's script prints it, its comment above its line, with no indent. A
        /// sub-flow step names the flow given, by name and Id.
        /// </summary>
        string WriteStep(FlowStep step, SubFlowReference? subFlow);
    }
}
