namespace Business.FlowScript.Syntax
{
    public interface IParser
    {
        /// <summary>
        /// A .sflw file as far as text alone can take it: names are still names, and an unreadable
        /// line is an entry in <see cref="FlowSyntax.Diagnostics"/> rather than an exception.
        /// </summary>
        FlowSyntax Read(string script);
    }
}
