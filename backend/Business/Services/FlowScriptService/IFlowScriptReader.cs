namespace Business.Services.FlowScriptService
{
    public interface IFlowScriptReader
    {
        /// <summary>
        /// A .sflw file as far as text alone can take it: names are still names, and an unreadable
        /// line is an entry in <see cref="FlowScriptDocument.Errors"/> rather than an exception.
        /// </summary>
        FlowScriptDocument Read(string script);
    }
}
