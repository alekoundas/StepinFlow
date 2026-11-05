using Core.Models.Dtos;

namespace Business.Ai
{
    public interface IFlowQuestionService
    {
        Task<AiChatAvailabilityDto> GetAvailabilityAsync(CancellationToken ct = default);

        /// <summary>Writes the answer to <paramref name="report"/> as it comes. Throws when the model cannot be reached or <paramref name="ct"/> stops it.</summary>
        Task AskAsync(AiChatRequestDto request, Action<AiChatEventDto> report, CancellationToken ct = default);
    }
}
