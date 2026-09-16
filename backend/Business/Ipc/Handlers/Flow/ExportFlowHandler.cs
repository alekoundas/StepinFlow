using Business.Services.FlowScriptService;
using Core.Models.Dtos;
using Core.Models.Ipc;
using MediatR;

namespace Business.Ipc.Handlers
{
    /// <summary>
    /// Writes a flow out as a `.sflw` script with its template images beside it.
    ///
    /// Thin on purpose: the loading, ordering and file naming are the exporter's, so the same work
    /// serves the fix loop, which needs the text and no files at all.
    /// </summary>
    public class ExportFlowHandler : IRequestHandler<ExportFlowCommand, ResultDto<FlowExportResultDto>>
    {
        private readonly IFlowScriptExporter _exporter;

        public ExportFlowHandler(IFlowScriptExporter exporter)
        {
            _exporter = exporter;
        }

        public async Task<ResultDto<FlowExportResultDto>> Handle(ExportFlowCommand request, CancellationToken ct)
        {
            try
            {
                FlowExportResultDto result = await _exporter.ExportAsync(request.dto.FlowId, request.dto.FolderPath, ct);

                return ResultDto<FlowExportResultDto>.Success(result);
            }
            catch (Exception ex)
            {
                // A folder that cannot be written is the common case and it is the user's to fix,
                // so it comes back as a message rather than an unhandled failure.
                return ResultDto<FlowExportResultDto>.Failure(ex.Message);
            }
        }
    }
}
