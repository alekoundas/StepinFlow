using Core.Models.Dtos.FlowScript;

namespace Business.FlowScript
{
    public interface IFlowScriptPreviewService
    {
        /// <summary>
        /// What importing a .sflw file would do, with nothing saved: read and scanned as an import
        /// reads it, its templates looked for beside it, and the flow it would replace or the name
        /// it would clash with.
        /// </summary>
        Task<FlowScriptPreviewDto> PreviewAsync(string scriptPath, CancellationToken ct = default);
    }
}
