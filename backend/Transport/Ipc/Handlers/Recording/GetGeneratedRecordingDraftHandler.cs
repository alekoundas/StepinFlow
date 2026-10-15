using Business.Recording.Generation;
using Core.Models.Dtos;

namespace Transport.Ipc.Handlers
{
    /// <summary>Nothing generated yet is an answer, not a failure.</summary>
    public class GetGeneratedRecordingDraftHandler
    {
        private readonly IFlowGenerationSessionService _generationSessionService;

        public GetGeneratedRecordingDraftHandler(IFlowGenerationSessionService generationSessionService)
        {
            _generationSessionService = generationSessionService;
        }

        public Task<ResultDto<FlowGenerationResultDto?>> HandleAsync(CancellationToken ct)
        {
            FlowGenerationResultDto? generated = _generationSessionService.GetResult();
            ResultDto<FlowGenerationResultDto?> result = ResultDto<FlowGenerationResultDto?>.Success(generated);

            return Task.FromResult(result);
        }
    }
}
