using Core.Ports;
using Core.Enums;
using Core.Enums.Business;
using Core.Models.Business;
using Core.Models.Database;

using Core.Helpers;

namespace Business.Executions.Workers
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

            VariableTranslationResult text = cache.TranslateVariables(step.KeyboardInputText);
            if (!text.IsTranslated)
                return Task.FromResult(ExecutionStep.Failure(VariableTranslator.DescribeUntranslated(text.Untranslated)));

            if (step.KeyboardInputType != KeyboardInputTypeEnum.COMBINATION)
            {
                _inputService.SimulateKeyboard(text.Text);

                return Task.FromResult(ExecutionStep.Success());
            }

            KeyboardKeyActionTypeEnum keyAction = step.KeyboardKeyActionType ?? KeyboardKeyActionTypeEnum.PRESS;
            if (keyAction != KeyboardKeyActionTypeEnum.PRESS)
                return Task.FromResult(HoldOrRelease(keyAction, text.Text));

            if (!KeyCombinationHelper.TryParse(text.Text, out List<KeyCodeEnum> modifiers, out KeyCodeEnum key))
                return Task.FromResult(ExecutionStep.Failure($"\"{text.Text}\" is not a key combination this can press."));

            _inputService.SimulateKeyCombination(modifiers, key);

            return Task.FromResult(ExecutionStep.Success(message: $"Pressed {text.Text}"));
        }


        // ================================================================
        // Private methods
        // ================================================================

        // Two halves of one gesture, as a held click is: whatever runs between them runs with the
        // keys down. The engine lets go of anything still held when the execution ends.
        private ExecutionStep HoldOrRelease(KeyboardKeyActionTypeEnum keyAction, string combination)
        {
            if (!KeyCombinationHelper.TryParseKeys(combination, out List<KeyCodeEnum> keys))
                return ExecutionStep.Failure($"\"{combination}\" is not a key combination this can hold.");

            if (keyAction == KeyboardKeyActionTypeEnum.HOLD)
            {
                _inputService.SimulateKeysDown(keys);
                return ExecutionStep.Success(message: $"Holding {combination}");
            }

            _inputService.SimulateKeysUp(keys);
            return ExecutionStep.Success(message: $"Released {combination}");
        }
    }
}
