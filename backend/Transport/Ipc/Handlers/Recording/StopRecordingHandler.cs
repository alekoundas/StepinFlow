using Business.Recording.Session;
using Core.Models.Dtos;

namespace Transport.Ipc.Handlers
{
    /// <summary>
    /// Stops and returns at once. The recording is then worked through in the background,
    /// RECORDING_PROGRESS says how far, and Recording.getResult has it once READY. A recording
    /// already stopped, by the bar or its hotkey, is left as it is.
    /// </summary>
    public class StopRecordingHandler
    {
        private readonly IRecordingSessionService _recordingSessionService;

        public StopRecordingHandler(IRecordingSessionService recordingSessionService)
        {
            _recordingSessionService = recordingSessionService;
        }

        public async Task<ResultDto<bool>> HandleAsync(CancellationToken ct)
        {
            await _recordingSessionService.StopAsync(ct);
            return ResultDto<bool>.Success(true);
        }
    }
}
