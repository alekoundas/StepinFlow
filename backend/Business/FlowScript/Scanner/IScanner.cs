using Business.FlowScript.Models.Binding;

namespace Business.FlowScript.Scanner
{
    public interface IScanner
    {
        /// <summary>
        /// Convert a .sflw file
        /// </summary>
        FlowScriptSchema Read(string script);
    }
}
