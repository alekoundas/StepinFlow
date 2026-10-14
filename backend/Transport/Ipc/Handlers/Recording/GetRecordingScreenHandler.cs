using Business.Recording.Session;
using Core.Models.Dtos;

namespace Transport.Ipc.Handlers
{
    /// <summary>No screen is an answer, not a failure: a recording with no main area keeps none.</summary>
    public class GetRecordingScreenHandler
    {
        private readonly IRecordingSessionService _recordingSessionService;

        public GetRecordingScreenHandler(IRecordingSessionService recordingSessionService)
        {
            _recordingSessionService = recordingSessionService;
        }

        public Task<ResultDto<RecordingScreenDto?>> HandleAsync(int actionIndex, CancellationToken ct)
        {
            RecordingScreenDto? screen = _recordingSessionService.GetScreen(actionIndex);
            ResultDto<RecordingScreenDto?> result = ResultDto<RecordingScreenDto?>.Success(screen);

            return Task.FromResult(result);
        }
    }
}
