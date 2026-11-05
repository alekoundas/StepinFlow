using Core.Models.Dtos;

namespace Business.Ai
{
    public interface IAiChatSessionService
    {
        /// <summary>Starts the answer and returns. AI_CHAT_EVENT says how it goes. False when that answer has already started.</summary>
        bool Start(AiChatRequestDto request);

        /// <summary>Nothing happens when that answer has already ended.</summary>
        void Stop(string answerId);
    }
}
