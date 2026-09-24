using Core.Models.Dtos;

namespace Business.Ai
{
    public interface IFlowQuestionService
    {
        Task<AiChatAvailabilityDto> GetAvailabilityAsync(CancellationToken ct = default);
        Task<AiChatAnswerDto> AskAsync(AiChatRequestDto request, CancellationToken ct = default);
    }
}
