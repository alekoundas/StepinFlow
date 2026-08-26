using Business.Services.SystemActionService;
using Core.Models.Business;
using Core.Models.Database;

namespace Business.Services.ExecutionService.Workers
{
    public class SystemActionStepWorker : IStepWorker
    {
        private readonly ISystemActionService _systemActionService;

        public SystemActionStepWorker(ISystemActionService systemActionService)
        {
            _systemActionService = systemActionService;
        }

        public Task<ExecutionStep> ExecuteAsync(FlowStep step, IExecutionCacheService cache, CancellationToken ct)
        {
            _systemActionService.Run(step.SystemActionType);
            return Task.FromResult(ExecutionStep.Success());
        }
    }
}
