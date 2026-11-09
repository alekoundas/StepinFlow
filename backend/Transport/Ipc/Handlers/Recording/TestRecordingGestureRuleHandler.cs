using Business.Recording.Session;
using Core.Models.Dtos;

namespace Transport.Ipc.Handlers
{
    public class TestRecordingGestureRuleHandler
    {
        private readonly IRecordingSessionService _recordingSessionService;

        public TestRecordingGestureRuleHandler(IRecordingSessionService recordingSessionService)
        {
            _recordingSessionService = recordingSessionService;
        }

        public Task<ResultDto<bool>> HandleAsync(RecordingGestureRuleDto dto, CancellationToken ct)
        {
            bool passes = _recordingSessionService.TestGestureRule(dto);
            return Task.FromResult(ResultDto<bool>.Success(passes));
        }
    }
}
