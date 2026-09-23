using Business.FlowScript;
using Business.FlowScript.Binding;
using Business.FlowScript.Syntax;
using Business.FlowScript.Text;
using Core.Models.Dtos;
using Transport.Messages;
using MediatR;

namespace Transport.Ipc.Handlers
{
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
                FlowExportResultDto result = await _exporter.ExportAsync(request.Dto.FlowId, request.Dto.FolderPath, ct);

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
