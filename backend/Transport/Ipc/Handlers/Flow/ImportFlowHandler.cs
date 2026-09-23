using Business.FlowScript;
using Business.FlowScript.Binding;
using Business.FlowScript.Syntax;
using Business.FlowScript.Text;
using Core.Models.Dtos;
using Core.Models.Ipc;
using MediatR;

namespace Business.Ipc.Handlers
{
    public class ImportFlowHandler : IRequestHandler<ImportFlowCommand, ResultDto<FlowImportResultDto>>
    {
        private readonly IFlowScriptImporter _importer;

        public ImportFlowHandler(IFlowScriptImporter importer)
        {
            _importer = importer;
        }

        public async Task<ResultDto<FlowImportResultDto>> Handle(ImportFlowCommand request, CancellationToken ct)
        {
            try
            {
                FlowImportResultDto result = await _importer.ImportAsync(request.Dto.ScriptPath, ct);

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
