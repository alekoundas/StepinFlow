using Business.Services.RecordingService;
using Core.Models.Dtos;

namespace Transport.Ipc.Handlers
{
    public class DiscardRecordingHandler
    {
        private readonly IRecordingSessionService _recordingSessionService;

        public DiscardRecordingHandler(IRecordingSessionService recordingSessionService)
        {
            _recordingSessionService = recordingSessionService;
        }

        public async Task<ResultDto<bool>> HandleAsync(CancellationToken ct)
        {
            if (_recordingSessionService.IsRecording)
                await _recordingSessionService.StopAsync(ct);

            _recordingSessionService.Clear();
            return ResultDto<bool>.Success(true);
        }
    }
}
