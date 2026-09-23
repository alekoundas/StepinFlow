using Business.Services.ExecutionService;
using Core.Models.Dtos;
using Core.Models.Ipc;

using MediatR;

namespace Business.Ipc.Handlers.Execution
{
    /// <summary>
    /// Read on mount. The engine outlives the page, so a run started before you navigated away is
    /// still going, and the page has to find that out rather than assume it is idle.
    /// </summary>
    public class GetExecutionStateHandler : IRequestHandler<GetExecutionStateQuery, ResultDto<ExecutionStateDto>>
    {
        private readonly IExecutionEngine _executionEngine;

        public GetExecutionStateHandler(IExecutionEngine executionEngine)
        {
            _executionEngine = executionEngine;
        }

        public Task<ResultDto<ExecutionStateDto>> Handle(GetExecutionStateQuery request, CancellationToken ct)
        {
            ExecutionStateDto dto = new ExecutionStateDto
            {
                State = _executionEngine.State,
                IsRunning = _executionEngine.IsRunning,
                ExecutionId = _executionEngine.ExecutionId,
                FlowId = _executionEngine.FlowId,
            };

            return Task.FromResult(ResultDto<ExecutionStateDto>.Success(dto));
        }
    }
}
