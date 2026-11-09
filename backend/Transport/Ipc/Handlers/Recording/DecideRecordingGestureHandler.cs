using Business.Recording.Session;
using Core.Models.Dtos;

namespace Transport.Ipc.Handlers
{
    public class DecideRecordingGestureHandler
    {
        private readonly IRecordingSessionService _recordingSessionService;

        public DecideRecordingGestureHandler(IRecordingSessionService recordingSessionService)
        {
            _recordingSessionService = recordingSessionService;
        }

        public Task<ResultDto<bool>> HandleAsync(RecordingGestureDecisionDto dto, CancellationToken ct)
        {
            ResultDto<bool> result = _recordingSessionService.DecideGesture(dto);
            return Task.FromResult(result);
        }
    }
}
