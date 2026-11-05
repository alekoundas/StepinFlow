using Business.Ai;
using Core.Models.Dtos;


namespace Transport.Ipc.Handlers.Ai
{
    public class StopAiAnswerHandler
    {
        private readonly IAiChatSessionService _chatSessionService;

        public StopAiAnswerHandler(IAiChatSessionService chatSessionService)
        {
            _chatSessionService = chatSessionService;
        }

        public Task<ResultDto<bool>> HandleAsync(string answerId, CancellationToken ct)
        {
            _chatSessionService.Stop(answerId);

            return Task.FromResult(ResultDto<bool>.Success(true));
        }
    }
}
