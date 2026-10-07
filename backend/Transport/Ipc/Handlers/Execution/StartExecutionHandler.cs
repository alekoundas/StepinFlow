using Business.Executions;
using Business.Validation;
using Core.Enums;
using Core.Models.Dtos;
using DataAccess;
using Microsoft.EntityFrameworkCore;

namespace Transport.Ipc.Handlers
{
    /// <summary>
    /// Starts an execution, unless the flow or a sub-flow it calls has a validation error. Refused
    /// here, with the reason, before the engine is asked - the engine runs whatever it is given.
    /// </summary>
    public class StartExecutionHandler
    {
        private readonly IExecutionEngine _executionEngine;
        private readonly IDbContextFactory<AppDbContext> _dbContextFactory;
        private readonly IFlowValidationService _flowValidationService;

        public StartExecutionHandler(IExecutionEngine executionEngine, IDbContextFactory<AppDbContext> dbContextFactory, IFlowValidationService flowValidationService)
        {
            _executionEngine = executionEngine;
            _dbContextFactory = dbContextFactory;
            _flowValidationService = flowValidationService;
        }

        public async Task<ResultDto<int>> HandleAsync(ExecutionStartDto dto, CancellationToken ct)
        {
            string? refusal = await RefusalAsync(dto.FlowId, ct);
            if (refusal != null)
                return ResultDto<int>.Failure(refusal);

            try
            {
                // Not the request's token: the run outlives the call that asked for it.
                int executionId = await _executionEngine.StartAsync(dto, CancellationToken.None);
                return ResultDto<int>.Success(executionId);
            }
            catch (InvalidOperationException ex)
            {
                return ResultDto<int>.Failure(ex.Message);
            }
        }


        // ================================================================
        // Private methods
        // ================================================================

        // The flow and every sub-flow it calls: a sub-flow with errors breaks the execution as surely.
        private async Task<string?> RefusalAsync(int flowId, CancellationToken ct)
        {
            await using AppDbContext dbContext = await _dbContextFactory.CreateDbContextAsync(ct);

            HashSet<int> validated = new HashSet<int>();
            Queue<int> pending = new Queue<int>();
            pending.Enqueue(flowId);

            while (pending.Count > 0)
            {
                int id = pending.Dequeue();
                if (!validated.Add(id))
                    continue;

                FlowValidationResultDto validation = await _flowValidationService.ValidateAsync(dbContext, id, ct);
                if (validation.HasErrors)
                    return await MessageAsync(dbContext, id, validation, ct);

                List<int> subFlowIds = await dbContext.FlowSteps
                    .AsNoTracking()
                    .Where(x => x.RootId == id && x.SubFlowId != null)
                    .Select(x => x.SubFlowId!.Value)
                    .ToListAsync(ct);

                foreach (int subFlowId in subFlowIds)
                    pending.Enqueue(subFlowId);
            }

            return null;
        }

        // Which flow, which step and the first thing wrong with it.
        private static async Task<string> MessageAsync(AppDbContext dbContext, int flowId, FlowValidationResultDto validation, CancellationToken ct)
        {
            string name = await dbContext.Flows.Where(x => x.Id == flowId).Select(x => x.Name).FirstOrDefaultAsync(ct) ?? "This flow";
            FlowValidationIssueDto first = validation.Issues.First(x => x.Severity == ValidationSeverityEnum.ERROR);

            string where = string.Empty;
            if (!string.IsNullOrWhiteSpace(first.FlowStepName))
                where = $" \"{first.FlowStepName}\":";

            return $"\"{name}\" has errors to fix before it can run.{where} {first.Message}";
        }
    }
}
