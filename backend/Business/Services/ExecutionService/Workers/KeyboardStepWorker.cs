using Business.Services.InputService;
using Core.Models.Business;
using Core.Models.Database;

namespace Business.Services.ExecutionService.Workers
{
    public class KeyboardStepWorker : IStepWorker
    {
        private readonly IInputService _inputService;

        public KeyboardStepWorker(IInputService inputService)
        {
            _inputService = inputService;
        }

        public Task<ExecutionStep> ExecuteAsync(FlowStep step, IExecutionCacheService cache, CancellationToken ct)
        {
            if (string.IsNullOrEmpty(step.KeyboardInputText))
                return Task.FromResult(ExecutionStep.Success());

            _inputService.SimulateKeyboard(step.KeyboardInputText);

            return Task.FromResult(ExecutionStep.Success());
        }
    }
}
