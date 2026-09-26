using Business.Executions;
using Business.Executions.Workers;
using Business.Tests.Fakes;
using Core.Enums;
using Core.Models.Database;
using static Business.Tests.TestToken;

namespace Business.Tests.Executions.Workers
{
    /// <summary>
    /// Typing text and sending key combinations. Both refuse rather than type something wrong: a
    /// variable nothing filled in, or a combination that names no key, presses nothing at all.
    /// </summary>
    public sealed class KeyboardStepWorkerTests
    {
        private readonly FakeInputService _input = new FakeInputService();

        private async Task<ExecutionStep> Keyboard(FlowStep step, ExecutionCacheService? cache = null)
        {
            return await new KeyboardStepWorker(_input).ExecuteAsync(step, cache ?? await WorkerCache.ForAsync(step), Ct);
        }

        [Fact]
        public async Task Text_is_typed_with_its_variables_filled_in()
        {
            FlowStep read = new FlowStep { Id = 2, Name = "username", FlowStepType = FlowStepTypeEnum.SEARCH_TEXT };
            FlowStep type = new FlowStep { Id = 1, FlowStepType = FlowStepTypeEnum.KEYBOARD_INPUT, KeyboardInputType = KeyboardInputTypeEnum.TEXT, KeyboardInputText = "hello {{username}}" };
            ExecutionCacheService cache = await WorkerCache.ForAsync(type, read);
            cache.Ran(read, value: "alex");

            await Keyboard(type, cache);

            _input.Actions.ShouldBe(["type hello alex"]);
        }

        // Typing "{{password}}" into a password box and calling it a pass is the failure this prevents.
        [Fact]
        public async Task Text_with_a_variable_nothing_has_filled_types_nothing_and_fails()
        {
            ExecutionStep result = await Keyboard(new FlowStep { Id = 1, FlowStepType = FlowStepTypeEnum.KEYBOARD_INPUT, KeyboardInputText = "{{password}}" });

            result.Outcome.ShouldBe(StepOutcomeEnum.FAILURE);
            _input.Actions.ShouldBeEmpty();
        }

        [Fact]
        public async Task A_combination_is_pressed_as_one()
        {
            await Keyboard(new FlowStep { Id = 1, FlowStepType = FlowStepTypeEnum.KEYBOARD_INPUT, KeyboardInputType = KeyboardInputTypeEnum.COMBINATION, KeyboardInputText = "Ctrl+Shift+T" });

            _input.Actions.ShouldBe(["press LeftCtrl+LeftShift+T"]);
        }

        [Fact]
        public async Task A_combination_that_is_not_one_fails_and_presses_nothing()
        {
            ExecutionStep result = await Keyboard(new FlowStep { Id = 1, FlowStepType = FlowStepTypeEnum.KEYBOARD_INPUT, KeyboardInputType = KeyboardInputTypeEnum.COMBINATION, KeyboardInputText = "Ctrl+Nope" });

            result.Outcome.ShouldBe(StepOutcomeEnum.FAILURE);
            _input.Actions.ShouldBeEmpty();
        }
    }
}
