using Business.Recording.Session;
using Core.Models.Dtos;

namespace Transport.Ipc.Handlers
{
    /// <summary>The recording bar's Stage button: where a stage of the test begins, for Review to name.</summary>
    public class MarkRecordingStageHandler
    {
        private readonly IRecordingSessionService _recordingSessionService;

        public MarkRecordingStageHandler(IRecordingSessionService recordingSessionService)
        {
            _recordingSessionService = recordingSessionService;
        }

        public Task<ResultDto<bool>> HandleAsync(CancellationToken ct)
        {
            if (!_recordingSessionService.IsRecording)
                return Task.FromResult(ResultDto<bool>.Failure("Nothing is being recorded."));

            _recordingSessionService.MarkStage();
            return Task.FromResult(ResultDto<bool>.Success(true));
        }
    }
}
