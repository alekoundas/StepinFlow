using Business.Recording;
using Core.Models.Dtos;

namespace Transport.Ipc.Handlers
{
    public class GetRecordingScreenshotHandler
    {
        private readonly IRecordingSessionService _recordingSessionService;

        public GetRecordingScreenshotHandler(IRecordingSessionService recordingSessionService)
        {
            _recordingSessionService = recordingSessionService;
        }

        public Task<ResultDto<byte[]>> HandleAsync(int index, CancellationToken ct)
        {
            byte[]? image = _recordingSessionService.GetScreenshot(index);

            return Task.FromResult(image == null
                ? ResultDto<byte[]>.Failure("That action has no screenshot.")
                : ResultDto<byte[]>.Success(image));
        }
    }
}
