using Business.FlowScript.Syntax;

namespace Business.FlowScript.Scanner
{
    public interface IScanner
    {
        /// <summary>
        /// Convert a .sflw file
        /// </summary>
        FlowSyntax Read(string script);
    }
}
