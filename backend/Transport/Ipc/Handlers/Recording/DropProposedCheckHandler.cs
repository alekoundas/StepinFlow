using Business.Recording.Generation;
using Business.Recording.Session;
using Core.Models.Dtos;

namespace Transport.Ipc.Handlers
{
    /// <summary>
    /// The Check page dropping a check the model added: out of the flow, with what was saved, and
    /// the checks still undecided as they read now.
    /// </summary>
    public class DropProposedCheckHandler
    {
        private readonly IRecordingSessionService _recordingSessionService;
        private readonly IFlowGenerationService _flowGenerationService;

        public DropProposedCheckHandler(IRecordingSessionService recordingSessionService, IFlowGenerationService flowGenerationService)
        {
            _recordingSessionService = recordingSessionService;
            _flowGenerationService = flowGenerationService;
        }

        public async Task<ResultDto<FlowGenerationResultDto>> HandleAsync(ProposedCheckDropDto dto, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(dto.Name))
                return ResultDto<FlowGenerationResultDto>.Failure("Say which check to drop.");

            List<string> screenLines = _recordingSessionService.GetResult()?.ScreenLines ?? [];
            return await _flowGenerationService.DropCheckAsync(dto, screenLines, ct);
        }
    }
}
