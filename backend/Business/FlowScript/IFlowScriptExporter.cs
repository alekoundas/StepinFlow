using Core.Models.Dtos.FlowScript;

namespace Business.FlowScript
{
    public interface IFlowScriptExporter
    {
        /// <summary> A flow as script text, with nothing written to disk.</summary>
        Task<string> RenderAsync(int flowId, CancellationToken ct = default);

        /// <summary> Writes the script and its template images in the app's export path.</summary>
        Task<FlowScriptExportResultDto> ExportAsync(int flowId, string? folderPath = null, CancellationToken ct = default);
    }
}
