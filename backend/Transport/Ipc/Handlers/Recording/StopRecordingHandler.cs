using Business.Recording;
using Core.Models.Business;
using Core.Models.Dtos;

namespace Transport.Ipc.Handlers
{
    /// <summary>
    /// Stops and coalesces in one call, so the wizard opens on actions rather than on raw input
    /// it would have to fold a second way.
    /// </summary>
    public class StopRecordingHandler
    {
        private readonly IRecordingSessionService _recordingSessionService;
        private readonly TimeProvider _timeProvider;

        public StopRecordingHandler(IRecordingSessionService recordingSessionService, TimeProvider timeProvider)
        {
            _recordingSessionService = recordingSessionService;
            _timeProvider = timeProvider;
        }

        public async Task<ResultDto<IReadOnlyList<RecordedActionDto>>> HandleAsync(CancellationToken ct)
        {
            IReadOnlyList<RecordedInput> events = await _recordingSessionService.StopAsync(ct);

            return ResultDto<IReadOnlyList<RecordedActionDto>>.Success(RecordingActionBuilder.Build(events, _timeProvider));
        }
    }
}
