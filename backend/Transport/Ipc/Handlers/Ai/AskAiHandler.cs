using Business.Ai;
using Core.Models.Dtos;


namespace Transport.Ipc.Handlers.Ai
{
    /// <summary>Starts an answer and comes straight back. How it goes arrives as AI_CHAT_EVENT.</summary>
    public class AskAiHandler
    {
        private readonly IAiChatSessionService _chatSessionService;

        public AskAiHandler(IAiChatSessionService chatSessionService)
        {
            _chatSessionService = chatSessionService;
        }

        public Task<ResultDto<bool>> HandleAsync(AiChatRequestDto dto, CancellationToken ct)
        {
            if (!_chatSessionService.Start(dto))
                return Task.FromResult(ResultDto<bool>.Failure("This answer has already started."));

            return Task.FromResult(ResultDto<bool>.Success(true));
        }
    }
}
