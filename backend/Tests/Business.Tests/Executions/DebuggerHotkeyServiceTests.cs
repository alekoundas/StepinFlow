using Business.Executions;
using Business.Tests.Fakes;
using Core.Catalogs;
using Core.Enums;

using static Business.Tests.TestToken;

namespace Business.Tests.Executions
{
    /// <summary>
    /// The debugger's keys come from the hook, so they work whatever window is in front. Each acts
    /// in the states its button on the execution page is live in. The defaults: Continue F5, Step
    /// into F11, Step over F10, Pause F9, Stop F8.
    /// </summary>
    public sealed class DebuggerHotkeyServiceTests
    {
        private readonly FakeExecutionEngine _engine = new FakeExecutionEngine { IsRunning = true, State = RunStateEnum.RUNNING };
        private readonly FakeInputRecordService _hook = new FakeInputRecordService();
        private readonly FakeAppSettingService _settings = new FakeAppSettingService();

        // The execution being listened through, which a test ends when it wants.
        private readonly TaskCompletionSource _execution = new TaskCompletionSource();
        private DebuggerHotkeyService? _service;
        private Task _listening = Task.CompletedTask;

        private async Task BindAsync()
        {
            _service = new DebuggerHotkeyService(_engine, _hook, _settings);
            await _service.BindAsync(Ct);
            _listening = _service.ListenUntilAsync(_execution.Task);
        }

        private async Task EndExecutionAsync()
        {
            _execution.SetResult();
            await _listening;
        }

        private void Tap(string key)
        {
            _hook.Press(key);
            _hook.Release(key);
        }

        [Theory]
        [InlineData("VcF5", RunStateEnum.PAUSED, "Continue")]
        [InlineData("VcF11", RunStateEnum.PAUSED, "StepInto")]
        [InlineData("VcF10", RunStateEnum.PAUSED, "StepOver")]
        [InlineData("VcF9", RunStateEnum.RUNNING, "Pause")]
        [InlineData("VcF8", RunStateEnum.RUNNING, "Stop")]
        [InlineData("VcF8", RunStateEnum.PAUSED, "Stop")]
        public async Task A_key_acts_on_the_execution_as_its_button_does(string key, RunStateEnum state, string command)
        {
            _engine.State = state;
            await BindAsync();

            Tap(key);

            _engine.Commands.ShouldBe([command]);
        }

        [Theory]
        [InlineData("VcF5", RunStateEnum.RUNNING)]
        [InlineData("VcF11", RunStateEnum.RUNNING)]
        [InlineData("VcF10", RunStateEnum.RUNNING)]
        [InlineData("VcF9", RunStateEnum.PAUSED)]
        [InlineData("VcF8", RunStateEnum.STOPPING)]
        public async Task A_key_whose_button_is_off_does_nothing(string key, RunStateEnum state)
        {
            _engine.State = state;
            await BindAsync();

            Tap(key);

            _engine.Commands.ShouldBeEmpty();
        }

        [Fact]
        public async Task Nothing_is_acted_on_while_no_flow_is_executing()
        {
            _engine.IsRunning = false;
            await BindAsync();

            Tap("VcF8");

            _engine.Commands.ShouldBeEmpty();
        }

        [Fact]
        public async Task Nothing_is_listened_for_before_an_execution_starts()
        {
            DebuggerHotkeyService service = new DebuggerHotkeyService(_engine, _hook, _settings);
            await service.BindAsync(Ct);

            Tap("VcF8");

            _hook.KeyListeners.ShouldBe(0);
            _engine.Commands.ShouldBeEmpty();
        }

        [Fact]
        public async Task Nothing_is_listened_for_once_the_execution_ends()
        {
            await BindAsync();
            _hook.KeyListeners.ShouldBe(1);

            await EndExecutionAsync();
            Tap("VcF8");

            _hook.KeyListeners.ShouldBe(0);
            _engine.Commands.ShouldBeEmpty();
        }

        // Its release came while nothing listened, so it is not still down next time.
        [Fact]
        public async Task A_key_let_go_between_two_executions_is_not_held_in_the_second()
        {
            await BindAsync();
            _engine.State = RunStateEnum.PAUSED;
            _hook.Press("VcF9");
            await EndExecutionAsync();

            _engine.State = RunStateEnum.RUNNING;
            _listening = _service!.ListenUntilAsync(new TaskCompletionSource().Task);
            Tap("VcF9");

            _engine.Commands.ShouldBe(["Pause"]);
        }

        [Fact]
        public async Task A_key_held_down_acts_once()
        {
            await BindAsync();

            _hook.Press("VcF9");
            _hook.Press("VcF9");
            _hook.Press("VcF9");

            _engine.Commands.ShouldBe(["Pause"]);
        }

        [Fact]
        public async Task A_combination_needs_exactly_its_keys_with_its_last_key_last()
        {
            await _settings.SetAsync(AppSettingCatalog.HotkeyStop.Key, "VcLeftControl+VcF8", Ct);
            await BindAsync();

            Tap("VcF8");
            _hook.Press("VcLeftShift");
            _hook.Press("VcLeftControl");
            Tap("VcF8");
            _hook.Release("VcLeftShift");
            Tap("VcF8");

            _engine.Commands.ShouldBe(["Stop"]);
        }

        [Fact]
        public async Task The_keys_are_the_ones_in_Settings_when_the_execution_started()
        {
            await _settings.SetAsync(AppSettingCatalog.HotkeyPause.Key, "VcF6", Ct);
            await BindAsync();

            Tap("VcF9");
            Tap("VcF6");

            _engine.Commands.ShouldBe(["Pause"]);
        }

        [Fact]
        public async Task A_key_cleared_in_Settings_is_unbound()
        {
            await _settings.SetAsync(AppSettingCatalog.HotkeyPause.Key, string.Empty, Ct);
            await BindAsync();

            Tap("VcF9");

            _engine.Commands.ShouldBeEmpty();
        }
    }
}
