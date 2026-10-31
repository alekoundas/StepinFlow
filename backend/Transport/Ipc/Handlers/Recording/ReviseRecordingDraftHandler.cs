using Business.Recording.Generation;
using Business.Recording.Session;
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
        private readonly IRecordingSessionService _recordingSessionService;

        public ReviseRecordingDraftHandler(IFlowGenerationSessionService generationSessionService, IRecordingSessionService recordingSessionService)
        {
            _generationSessionService = generationSessionService;
            _recordingSessionService = recordingSessionService;
        }

        public async Task<ResultDto<bool>> HandleAsync(RecordingDraftRevisionDto dto, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(dto.Changes))
                return ResultDto<bool>.Failure("Write what should change first.");

            // The recording the flow came from, while it is still held: what the screen said there.
            List<string> screenLines = _recordingSessionService.GetResult()?.ScreenLines ?? [];

            return await _generationSessionService.StartRevisionAsync(dto, screenLines);
        }
    }
}
