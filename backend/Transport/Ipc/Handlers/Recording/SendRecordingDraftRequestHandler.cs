using Business.Recording.Generation;
using Core.Models.Dtos;

namespace Transport.Ipc.Handlers
{
    /// <summary>The tester has read the request shown before a cloud model is sent it, and sends it.</summary>
    public class SendRecordingDraftRequestHandler
    {
        private readonly IFlowGenerationSessionService _generationSessionService;

        public SendRecordingDraftRequestHandler(IFlowGenerationSessionService generationSessionService)
        {
            _generationSessionService = generationSessionService;
        }

        public Task<ResultDto<bool>> HandleAsync(CancellationToken ct)
        {
            return Task.FromResult(_generationSessionService.Send());
        }
    }
}
