using Business.FlowScript;
using Core.Models.Dtos;
using Core.Models.Dtos.FlowScript;

namespace Transport.Ipc.Handlers
{
    public class ExportFlowScriptHandler
    {
        private readonly IFlowScriptExporter _exporter;

        public ExportFlowScriptHandler(IFlowScriptExporter exporter)
        {
            _exporter = exporter;
        }

        public async Task<ResultDto<FlowScriptExportResultDto>> HandleAsync(FlowScriptExportRequestDto dto, CancellationToken ct)
        {
            try
            {
                FlowScriptExportResultDto result = await _exporter.ExportAsync(dto.FlowId, dto.FolderPath, ct);

                return ResultDto<FlowScriptExportResultDto>.Success(result);
            }
            catch (Exception ex)
            {
                // A folder that cannot be written is the common case and it is the user's to fix,
                // so it comes back as a message rather than an unhandled failure.
                return ResultDto<FlowScriptExportResultDto>.Failure(ex.Message);
            }
        }
    }
}
