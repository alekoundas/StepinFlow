using Business.Recording.Session;
using Core.Models.Dtos;

namespace Transport.Ipc.Handlers
{
    /// <summary>Sent once, as the tester continues from Review, rather than once per change.</summary>
    public class ReviewRecordingHandler
    {
        private readonly IRecordingSessionService _recordingSessionService;

        public ReviewRecordingHandler(IRecordingSessionService recordingSessionService)
        {
            _recordingSessionService = recordingSessionService;
        }

        public Task<ResultDto<bool>> HandleAsync(ReviewedRecordingDto dto, CancellationToken ct)
        {
            ResultDto<bool> result = _recordingSessionService.Review(dto);
            return Task.FromResult(result);
        }
    }
}
