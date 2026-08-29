using Core.Models.Dtos;
using Core.Models.Ipc;

using DataAccess;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Business.Ipc.Handlers.Execution
{
    /// <summary>
    /// One run and every step of it, in the order they happened. Projected rather than mapped: the
    /// page reads a flat list and indents it by Depth, so nothing needs loading through a relation.
    /// </summary>
    public class GetExecutionHandler : IRequestHandler<GetExecutionQuery, ResultDto<ExecutionDto>>
    {
        private readonly IDbContextFactory<AppDbContext> _dbContextFactory;

        public GetExecutionHandler(IDbContextFactory<AppDbContext> dbContextFactory)
        {
            _dbContextFactory = dbContextFactory;
        }

        public async Task<ResultDto<ExecutionDto>> Handle(GetExecutionQuery request, CancellationToken ct)
        {
            await using AppDbContext dbContext = await _dbContextFactory.CreateDbContextAsync(ct);

            ExecutionDto? execution = await dbContext.Executions
                .AsNoTracking()
                .Where(x => x.Id == request.id)
                .Select(x => new ExecutionDto
                {
                    Id = x.Id,
                    CreatedOn = x.CreatedOn,
                    CompletedAt = x.CompletedAt,
                    Status = x.Status,
                    HistoryLevel = x.HistoryLevel,
                    StepCount = x.StepCount,
                    ErrorFlowStepId = x.ErrorFlowStepId,
                    ErrorMessage = x.ErrorMessage,
                    FlowStructureHash = x.FlowStructureHash,
                    FlowId = x.FlowId,
                })
                .FirstOrDefaultAsync(ct);

            if (execution == null)
                return ResultDto<ExecutionDto>.Failure("That run no longer exists.");

            execution.ExecutionSteps = await dbContext.ExecutionSteps
                .AsNoTracking()
                .Where(x => x.ExecutionId == request.id)
                .OrderBy(x => x.Sequence)
                .Select(x => new ExecutionStepDto
                {
                    Id = x.Id,
                    Sequence = x.Sequence,
                    ParentSequence = x.ParentSequence,
                    Depth = x.Depth,
                    LoopPass = x.LoopPass,
                    Name = x.Name,
                    FlowStepType = x.FlowStepType,
                    Outcome = x.Outcome,
                    StartedOn = x.StartedOn,
                    DurationMilliseconds = x.DurationMilliseconds,
                    ResultLocationX = x.ResultLocationX,
                    ResultLocationY = x.ResultLocationY,
                    MatchIndex = x.MatchIndex,
                    MatchCount = x.MatchCount,
                    Value = x.Value,
                    Message = x.Message,
                    ExitCode = x.ExitCode,
                    Error = x.Error,
                    Command = x.Command,
                    ResultImagePath = x.ResultImagePath,
                    ExecutionId = x.ExecutionId,
                    FlowStepId = x.FlowStepId,
                })
                .ToListAsync(ct);

            return ResultDto<ExecutionDto>.Success(execution);
        }
    }
}
