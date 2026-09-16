namespace Business.Services.FlowScriptService
{
    public interface IFlowScriptWriter
    {
        /// <summary>
        /// A flow as the text that goes in a repository. Deterministic: the same flow writes the
        /// same bytes, which is what makes a round trip testable.
        /// </summary>
        string Write(FlowScriptSource source);
    }
}
