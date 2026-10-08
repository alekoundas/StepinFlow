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
        /// The same, from text that is not on disk yet - what a model hands back after editing a
        /// script. Each template's image is taken from <paramref name="templates"/> by its name.
        /// </summary>
        Task<FlowScriptImportResultDto> ImportTextAsync(string script, IReadOnlyDictionary<string, byte[]> templates, CancellationToken ct = default);
    }
}
