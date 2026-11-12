using Core.Models.Database;

namespace Business.Ai.AiDocuments
{
    public interface IFlowExampleIndex
    {
        /// <summary>
        /// The example document nearest the flow by what it is for and where it works, by meaning.
        /// The rule FlowExamples.DocumentFor picks by when there is too little to go on, or no
        /// embedding model.
        /// </summary>
        string DocumentFor(Flow flow, FlowArea? mainArea);
    }
}
