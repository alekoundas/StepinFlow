using Business.FlowScript;
using Core.Models.Dtos;
using Core.Models.Dtos.FlowScript;

namespace Transport.Ipc.Handlers
{
    public class ImportFlowScriptHandler
    {
        private readonly IFlowScriptImporter _importer;

        public ImportFlowScriptHandler(IFlowScriptImporter importer)
        {
            _importer = importer;
        }

        public async Task<ResultDto<FlowScriptImportResultDto>> HandleAsync(FlowScriptImportRequestDto dto, CancellationToken ct)
        {
            try
            {
                FlowScriptImportResultDto result = await _importer.ImportAsync(dto.ScriptPath, ct);

                // A file that will not parse is not a failure of the call: the errors carry lines
                // and the editor puts them next to the text, so the result comes back as success.
                return ResultDto<FlowScriptImportResultDto>.Success(result);
            }
            catch (Exception ex)
            {
                return ResultDto<FlowScriptImportResultDto>.Failure(ex.Message);
            }
        }
    }
}
