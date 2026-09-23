using Business.Services.Ai;
using Core.Models.Dtos;


namespace Transport.Ipc.Handlers.Ai
{
    /// <summary>Answers a question about the flows, by letting the model query the database.</summary>
    public class AskAiHandler
    {
        private readonly IFlowQuestionService _flowQuestionService;

        public AskAiHandler(IFlowQuestionService flowQuestionService)
        {
            _flowQuestionService = flowQuestionService;
        }

        public async Task<ResultDto<AiChatAnswerDto>> HandleAsync(AiChatRequestDto dto, CancellationToken ct)
        {
            return ResultDto<AiChatAnswerDto>.Success(await _flowQuestionService.AskAsync(dto, ct));
        }
    }
}
