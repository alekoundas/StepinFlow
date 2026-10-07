using Core.Models.Dtos.FlowScript;

namespace Business.FlowScript
{
    public interface IFlowScriptImporter
    {
        /// <summary>
        /// Replaces a flow with what a .sflw file says it is. Parse, validate, then replace: a
        /// typo leaves the existing flow untouched rather than half written.
        /// </summary>
        Task<FlowScriptImportResultDto> ImportAsync(string scriptPath, CancellationToken ct = default);

        /// <summary>
        /// The same, from text that is not on disk yet - what the fix loop hands back after editing
        /// a script. Templates are read from <paramref name="templateFolderPath"/> when given.
        /// </summary>
        Task<FlowScriptImportResultDto> ImportTextAsync(string script, string? templateFolderPath, CancellationToken ct = default);
    }
}
