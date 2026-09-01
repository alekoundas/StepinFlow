using Core.Models.Dtos;

namespace Business.Services.Ai
{
    public interface IExecutionRunExplainService
    {
        Task<AiAnswerDto> ExplainExecutionAsync(int executionId, CancellationToken ct = default);
    }
}
