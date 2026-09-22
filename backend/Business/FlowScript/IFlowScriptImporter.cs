using Core.Models.Dtos;

namespace Business.FlowScript
{
    public interface IFlowScriptImporter
    {
        /// <summary>
        /// Replaces a flow with what a .sflw file says it is. Parse, validate, then replace: a
        /// typo leaves the existing flow untouched rather than half written.
        /// </summary>
        Task<FlowImportResultDto> ImportAsync(string scriptPath, CancellationToken ct = default);

        /// <summary>
        /// The same, from text that is not on disk yet - what the fix loop hands back after editing
        /// a script. Templates are read from <paramref name="templateFolderPath"/> when given.
        /// </summary>
        Task<FlowImportResultDto> ImportTextAsync(string script, string? templateFolderPath, CancellationToken ct = default);
    }
}
