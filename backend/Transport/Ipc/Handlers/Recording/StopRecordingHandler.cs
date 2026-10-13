using Business.Recording.Session;
using Core.Models.Dtos;

namespace Transport.Ipc.Handlers
{
    /// <summary>
    /// Stops and returns the actions in one call, so the wizard opens on actions rather than on
    /// raw input it would have to fold a second way. A recording already stopped, by the bar or
    /// its hotkey, returns the same actions again.
    /// </summary>
    public class StopRecordingHandler
    {
        private readonly IRecordingSessionService _recordingSessionService;

        public StopRecordingHandler(IRecordingSessionService recordingSessionService)
        {
            _recordingSessionService = recordingSessionService;
        }

        public async Task<ResultDto<IReadOnlyList<RecordedActionDto>>> HandleAsync(CancellationToken ct)
        {
            await _recordingSessionService.StopAsync(ct);
            IReadOnlyList<RecordedActionDto> actions = _recordingSessionService.GetActions();

            return ResultDto<IReadOnlyList<RecordedActionDto>>.Success(actions);
        }
    }
}
