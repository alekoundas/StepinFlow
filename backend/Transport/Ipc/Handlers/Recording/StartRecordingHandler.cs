using Business.Recording;
using Core.Models.Dtos;

namespace Transport.Ipc.Handlers
{
    public class StartRecordingHandler
    {
        private readonly IRecordingSessionService _recordingSessionService;

        public StartRecordingHandler(IRecordingSessionService recordingSessionService)
        {
            _recordingSessionService = recordingSessionService;
        }

        public async Task<ResultDto<bool>> HandleAsync(CancellationToken ct)
        {
            bool started = await _recordingSessionService.StartAsync(ct);

            return started
                ? ResultDto<bool>.Success(true)
                : ResultDto<bool>.Failure("A recording is already running.");
        }
    }
}
