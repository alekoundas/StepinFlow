using Business.Recording.Session;
using Core.Models.Dtos;

namespace Transport.Ipc.Handlers
{
    public class ResumeRecordingHandler
    {
        private readonly IRecordingSessionService _recordingSessionService;

        public ResumeRecordingHandler(IRecordingSessionService recordingSessionService)
        {
            _recordingSessionService = recordingSessionService;
        }

        public Task<ResultDto<bool>> HandleAsync(CancellationToken ct)
        {
            if (!_recordingSessionService.IsRecording)
                return Task.FromResult(ResultDto<bool>.Failure("Nothing is being recorded."));

            _recordingSessionService.Resume();
            return Task.FromResult(ResultDto<bool>.Success(true));
        }
    }
}
