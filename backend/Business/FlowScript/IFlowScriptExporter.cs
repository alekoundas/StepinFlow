using Business.FlowScript.Models;
using Core.Models.Dtos.FlowScript;

namespace Business.FlowScript
{
    public interface IFlowScriptExporter
    {
        /// <summary> A flow as script text, with nothing written to disk.</summary>
        Task<string> RenderAsync(int flowId, CancellationToken ct = default);

        /// <summary>A flow as the linked rows a script reads into, template images included: what a draft is built on.</summary>
        Task<FlowScriptSchema> LoadAsync(int flowId, CancellationToken ct = default);

        /// <summary> Writes the script and its template images in the app's export path.</summary>
        Task<FlowScriptExportResultDto> ExportAsync(int flowId, string? folderPath = null, CancellationToken ct = default);

        /// <summary>The .sflw files in a folder, newest first, each with the templates in the folder beside it.</summary>
        List<FlowScriptFileDto> ListScripts(string folderPath);
    }
}
