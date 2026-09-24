using Core.Models.Dtos;

namespace Business.Ai
{
    public interface IExecutionRunExplainService
    {
        Task<AiAnswerDto> ExplainExecutionAsync(int executionId, CancellationToken ct = default);
    }
}
