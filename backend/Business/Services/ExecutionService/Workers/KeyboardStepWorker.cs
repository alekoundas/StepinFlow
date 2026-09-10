using Business.Services.InputService;
using Core.Enums;
using Core.Models.Business;
using Core.Models.Database;

using SharpHook.Data;

using Core.Helpers;

namespace Business.Services.ExecutionService.Workers
{
    /// <summary>
    /// Typing, and shortcuts.
    ///
    /// The two are not the same thing said differently: "Ctrl+V" typed as text puts the six
    /// characters into whatever has focus, where pressed as keys it pastes.
    /// </summary>
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

            VariableTranslationResult text = cache.ResolveVariables(step.KeyboardInputText);
            if (!text.IsResolved)
                return Task.FromResult(ExecutionStep.Failure(VariableTranslator.DescribeUnresolved(text.Unresolved)));

            if (step.KeyboardInputType != KeyboardInputTypeEnum.COMBINATION)
            {
                _inputService.SimulateKeyboard(text.Text);

                return Task.FromResult(ExecutionStep.Success());
            }

            if (!KeyCombinationHelper.TryParse(text.Text, out List<KeyCode> modifiers, out KeyCode key))
                return Task.FromResult(ExecutionStep.Failure($"\"{text.Text}\" is not a key combination this can press."));

            _inputService.SimulateKeyCombination(modifiers, key);

            return Task.FromResult(ExecutionStep.Success(message: $"Pressed {text.Text}"));
        }
    }
}
