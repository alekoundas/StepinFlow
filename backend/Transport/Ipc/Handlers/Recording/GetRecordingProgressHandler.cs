using Business.Recording.Session;
using Core.Models.Dtos;

namespace Transport.Ipc.Handlers
{
    /// <summary>The recording bar opens after the recording started, so it asks once and then listens.</summary>
    public class GetRecordingProgressHandler
    {
        private readonly IRecordingSessionService _recordingSessionService;

        public GetRecordingProgressHandler(IRecordingSessionService recordingSessionService)
        {
            _recordingSessionService = recordingSessionService;
        }

        public Task<ResultDto<RecordingProgressDto>> HandleAsync(CancellationToken ct)
        {
            RecordingProgressDto progress = _recordingSessionService.GetProgress();
            ResultDto<RecordingProgressDto> result = ResultDto<RecordingProgressDto>.Success(progress);

            return Task.FromResult(result);
        }
    }
}
