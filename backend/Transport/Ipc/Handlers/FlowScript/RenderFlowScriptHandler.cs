using Business.FlowScript;
using Core.Models.Dtos;

namespace Transport.Ipc.Handlers
{
    /// <summary>The flow as Export writes it, for the editor's Script tab. Nothing is written to disk.</summary>
    public class RenderFlowScriptHandler
    {
        private readonly IFlowScriptExporter _exporter;

        public RenderFlowScriptHandler(IFlowScriptExporter exporter)
        {
            _exporter = exporter;
        }

        public async Task<ResultDto<string>> HandleAsync(int flowId, CancellationToken ct)
        {
            string script = await _exporter.RenderAsync(flowId, ct);
            return ResultDto<string>.Success(script);
        }
    }
}
