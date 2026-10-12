using Core.Catalogs;
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
        private readonly IInputSimulatorService _inputSimulatorService;

        public KeyboardStepWorker(IInputSimulatorService inputSimulatorService)
        {
            _inputSimulatorService = inputSimulatorService;
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
                _inputSimulatorService.SimulateKeyboard(text.Text);

                return Task.FromResult(ExecutionStep.Success());
            }

            KeyboardKeyActionTypeEnum keyAction = step.KeyboardKeyActionType ?? KeyboardKeyActionTypeEnum.PRESS;
            if (keyAction != KeyboardKeyActionTypeEnum.PRESS)
                return Task.FromResult(HoldOrRelease(keyAction, text.Text));

            if (!KeyCatalog.TryParse(text.Text, out List<KeyCodeEnum> modifiers, out KeyCodeEnum key))
                return Task.FromResult(ExecutionStep.Failure($"\"{text.Text}\" is not a key combination this can press."));

            _inputSimulatorService.SimulateKeyCombination(modifiers, key);

            return Task.FromResult(ExecutionStep.Success(message: $"Pressed {text.Text}"));
        }


        // ================================================================
        // Private methods
        // ================================================================

        // Two halves of one gesture, as a held click is: whatever runs between them runs with the
        // keys down. The engine lets go of anything still held when the execution ends.
        private ExecutionStep HoldOrRelease(KeyboardKeyActionTypeEnum keyAction, string combination)
        {
            if (!KeyCatalog.TryParseKeys(combination, out List<KeyCodeEnum> keys))
                return ExecutionStep.Failure($"\"{combination}\" is not a key combination this can hold.");

            if (keyAction == KeyboardKeyActionTypeEnum.HOLD)
            {
                _inputSimulatorService.SimulateKeysDown(keys);
                return ExecutionStep.Success(message: $"Holding {combination}");
            }

            _inputSimulatorService.SimulateKeysUp(keys);
            return ExecutionStep.Success(message: $"Released {combination}");
        }
    }
}
