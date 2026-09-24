using Business.Ai;
using Core.Models.Dtos;


namespace Transport.Ipc.Handlers.Ai
{
    /// <summary>Reads a run and says what went wrong.</summary>
    public class ExplainExecutionHandler
    {
        private readonly IExecutionRunExplainService _runExplainService;

        public ExplainExecutionHandler(IExecutionRunExplainService runExplainService)
        {
            _runExplainService = runExplainService;
        }

        public async Task<ResultDto<AiAnswerDto>> HandleAsync(int executionId, CancellationToken ct)
        {
            AiAnswerDto answer = await _runExplainService.ExplainExecutionAsync(executionId, ct);
            return ResultDto<AiAnswerDto>.Success(answer);
        }
    }
}
