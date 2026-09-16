using Core.Models.Dtos;

namespace Business.Services.FlowScriptService
{
    public interface IFlowScriptExporter
    {
        /// <summary>
        /// A flow as script text, with nothing written to disk.
        ///
        /// Separate from <see cref="ExportAsync"/> because the fix loop needs the same text to hand
        /// a model, and asking it to write a folder of images first would be absurd. Template file
        /// names are still resolved, so the text is identical either way.
        /// </summary>
        Task<string> RenderAsync(int flowId, CancellationToken ct = default);

        /// <summary>
        /// Writes the script and its template images. The folder defaults to the app's export path.
        /// </summary>
        Task<FlowExportResultDto> ExportAsync(int flowId, string? folderPath = null, CancellationToken ct = default);
    }
}
