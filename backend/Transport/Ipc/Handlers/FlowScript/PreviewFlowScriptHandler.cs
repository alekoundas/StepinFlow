using Business.FlowScript;
using Core.Models.Dtos;
using Core.Models.Dtos.FlowScript;

namespace Transport.Ipc.Handlers
{
    /// <summary>What importing a file would do. A file that does not read cleanly says so in its errors.</summary>
    public class PreviewFlowScriptHandler
    {
        private readonly IFlowScriptPreviewService _previewService;

        public PreviewFlowScriptHandler(IFlowScriptPreviewService previewService)
        {
            _previewService = previewService;
        }

        public async Task<ResultDto<FlowScriptPreviewDto>> HandleAsync(FlowScriptImportRequestDto dto, CancellationToken ct)
        {
            try
            {
                FlowScriptPreviewDto preview = await _previewService.PreviewAsync(dto.ScriptPath, ct);

                return ResultDto<FlowScriptPreviewDto>.Success(preview);
            }
            catch (Exception ex)
            {
                // A file that cannot be read - locked, or not allowed - is the user's to fix.
                return ResultDto<FlowScriptPreviewDto>.Failure(ex.Message);
            }
        }
    }
}
