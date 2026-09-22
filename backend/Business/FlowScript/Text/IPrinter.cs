using Business.FlowScript.Binding;

namespace Business.FlowScript.Text
{
    public interface IPrinter
    {
        /// <summary>
        /// A flow as the text that goes in a repository. Deterministic: the same flow writes the
        /// same bytes, which is what makes a round trip testable.
        /// </summary>
        string Write(BoundFlow source);
    }
}
