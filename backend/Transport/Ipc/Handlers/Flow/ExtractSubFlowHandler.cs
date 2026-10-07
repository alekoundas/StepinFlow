using Business.Flows.DataService;
using Core.Models.Dtos;

namespace Transport.Ipc.Handlers
{
    /// <summary>
    /// Lifts a step and everything under it into a new sub-flow, leaving a SUB_FLOW step in its place.
    /// 
    /// The rows are moved, not copied. Ids survive, so a reference between two extracted steps
    /// keeps resolving with no remapping and the template images follow their step untouched.
    /// Only RootId, and the parentage of the step at the top, change. That is also why a
    /// reference crossing the boundary is the one case that has to be refused rather than fixed
    /// up: everything else takes care of itself.
    ///
    /// Search areas and points are the exception. Those belong to a flow, so the ones the moved
    /// steps use are copied into the new sub-flow and the steps repointed, which is what makes it
    /// self contained enough to call from anywhere.
    /// </summary>
    public class ExtractSubFlowHandler
    {
        private readonly DataService _dataService;

        public ExtractSubFlowHandler(DataService dataService)
        {
            _dataService = dataService;
        }

        public async Task<ResultDto<ExtractSubFlowResultDto>> HandleAsync(ExtractSubFlowDto dto, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(dto.Name))
                return ResultDto<ExtractSubFlowResultDto>.Failure("Give the sub-flow a name.");

            try
            {
                return await _dataService.Flow.ExtractSubFlowAsync(dto, ct);
            }
            catch (Exception ex)
            {
                return ResultDto<ExtractSubFlowResultDto>.Failure(ex.Message);
            }
        }
    }
}
