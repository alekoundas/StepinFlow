using Business.Recording.Session;
using Core.Models.Dtos;

namespace Transport.Ipc.Handlers
{
    /// <summary>Asked once RECORDING_PROGRESS says READY. Too big for a broadcast, and not to be missed.</summary>
    public class GetRecordingResultHandler
    {
        private readonly IRecordingSessionService _recordingSessionService;

        public GetRecordingResultHandler(IRecordingSessionService recordingSessionService)
        {
            _recordingSessionService = recordingSessionService;
        }

        public Task<ResultDto<RecordingResultDto>> HandleAsync(CancellationToken ct)
        {
            RecordingResultDto? recording = _recordingSessionService.GetResult();

            ResultDto<RecordingResultDto> result = ResultDto<RecordingResultDto>.Failure("The recording is not ready yet.");
            if (recording != null)
                result = ResultDto<RecordingResultDto>.Success(recording);

            return Task.FromResult(result);
        }
    }
}
