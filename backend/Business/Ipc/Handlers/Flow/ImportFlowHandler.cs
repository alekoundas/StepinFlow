using Business.Services.FlowScriptService;
using Core.Models.Dtos;
using Core.Models.Ipc;
using MediatR;

namespace Business.Ipc.Handlers
{
    /// <summary>
    /// Replaces a flow with what a `.sflw` file says it is.
    ///
    /// Thin like its opposite number: parsing, validating and replacing are the importer's, and
    /// the same work serves the fix loop, which imports text that was never on disk.
    /// </summary>
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
