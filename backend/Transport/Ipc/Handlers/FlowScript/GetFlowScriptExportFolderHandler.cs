using Business.FlowScript;
using Core.Helpers;
using Core.Models.Dtos;
using Core.Models.Dtos.FlowScript;

namespace Transport.Ipc.Handlers
{
    /// <summary>Where an export goes unless told otherwise, and the scripts already there.</summary>
    public class GetFlowScriptExportFolderHandler
    {
        private readonly IFlowScriptExporter _exporter;

        public GetFlowScriptExportFolderHandler(IFlowScriptExporter exporter)
        {
            _exporter = exporter;
        }

        public Task<ResultDto<FlowScriptExportFolderDto>> HandleAsync(CancellationToken ct)
        {
            string folderPath = PathHelper.GetExportDataPath();

            FlowScriptExportFolderDto folder = new FlowScriptExportFolderDto
            {
                FolderPath = folderPath,
                Scripts = _exporter.ListScripts(folderPath),
            };

            return Task.FromResult(ResultDto<FlowScriptExportFolderDto>.Success(folder));
        }
    }
}
