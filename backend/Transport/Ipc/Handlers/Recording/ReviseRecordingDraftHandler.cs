using Business.Recording.Generation;
using Core.Models.Dtos;

namespace Transport.Ipc.Handlers
{
    /// <summary>
    /// The Check page's "Ask for changes": the flow as it is, revised by the model as the tester
    /// wrote. Starts it and returns; FLOW_GENERATION_PROGRESS says how far.
    /// </summary>
    public class ReviseRecordingDraftHandler
    {
        private readonly IFlowGenerationSessionService _generationSessionService;

        public ReviseRecordingDraftHandler(IFlowGenerationSessionService generationSessionService)
        {
            _generationSessionService = generationSessionService;
        }

        public async Task<ResultDto<bool>> HandleAsync(RecordingDraftRevisionDto dto, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(dto.Changes))
                return ResultDto<bool>.Failure("Write what should change first.");

            return await _generationSessionService.StartRevisionAsync(dto.FlowId, dto.Changes);
        }
    }
}
