using Business.Recording.Session;
using Core.Models.Dtos;

namespace Transport.Ipc.Handlers
{
    /// <summary>No gesture is an answer, not a failure: the question may have closed meanwhile.</summary>
    public class GetRecordingGestureHandler
    {
        private readonly IRecordingSessionService _recordingSessionService;

        public GetRecordingGestureHandler(IRecordingSessionService recordingSessionService)
        {
            _recordingSessionService = recordingSessionService;
        }

        public Task<ResultDto<RecordingGestureDto?>> HandleAsync(CancellationToken ct)
        {
            RecordingGestureDto? gesture = _recordingSessionService.GetGesture();
            ResultDto<RecordingGestureDto?> result = ResultDto<RecordingGestureDto?>.Success(gesture);

            return Task.FromResult(result);
        }
    }
}
