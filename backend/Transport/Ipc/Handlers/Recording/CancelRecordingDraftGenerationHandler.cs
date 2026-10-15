using Business.Recording.Generation;
using Core.Models.Dtos;

namespace Transport.Ipc.Handlers
{
    public class CancelRecordingDraftGenerationHandler
    {
        private readonly IFlowGenerationSessionService _generationSessionService;

        public CancelRecordingDraftGenerationHandler(IFlowGenerationSessionService generationSessionService)
        {
            _generationSessionService = generationSessionService;
        }

        public async Task<ResultDto<bool>> HandleAsync(CancellationToken ct)
        {
            await _generationSessionService.CancelAsync();
            return ResultDto<bool>.Success(true);
        }
    }
}
