using Business.Services.Ai;
using Business.Services.Ai.Providers;
using Core.Models.Dtos;


namespace Transport.Ipc.Handlers.Ai
{
    /// <summary>Whether the chat can be offered, and why not when it cannot.</summary>
    public class GetAiChatAvailabilityHandler
    {
        private readonly IFlowQuestionService _flowQuestionService;

        public GetAiChatAvailabilityHandler(IFlowQuestionService flowQuestionService)
        {
            _flowQuestionService = flowQuestionService;
        }

        public async Task<ResultDto<AiChatAvailabilityDto>> HandleAsync(CancellationToken ct)
        {
            return ResultDto<AiChatAvailabilityDto>.Success(await _flowQuestionService.GetAvailabilityAsync(ct));
        }
    }
}
