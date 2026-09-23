using Business.FlowScript;
using Business.FlowScript.Binding;
using Business.FlowScript.Syntax;
using Business.FlowScript.Text;
using Core.Models.Dtos;

namespace Transport.Ipc.Handlers
{
    public class ImportFlowHandler
    {
        private readonly IFlowScriptImporter _importer;

        public ImportFlowHandler(IFlowScriptImporter importer)
        {
            _importer = importer;
        }

        public async Task<ResultDto<FlowImportResultDto>> HandleAsync(FlowImportRequestDto dto, CancellationToken ct)
        {
            try
            {
                FlowImportResultDto result = await _importer.ImportAsync(dto.ScriptPath, ct);

                // A file that will not parse is not a failure of the call: the errors carry lines
                // and the editor puts them next to the text, so the result comes back as success.
                return ResultDto<FlowImportResultDto>.Success(result);
            }
            catch (Exception ex)
            {
                return ResultDto<FlowImportResultDto>.Failure(ex.Message);
            }
        }
    }
}
