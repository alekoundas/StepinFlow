using System.Drawing;

using Business.Executions;
using Business.Executions.Workers;
using Business.Tests.Fakes;
using Core.Enums;
using Core.Models.Business;
using Core.Models.Database;

namespace Business.Tests.Executions.Workers
{
    public sealed class InputWorkerTests
    {
        // ================================================================
        // Fakes
        // ================================================================
        private readonly FakeInputService _input = new FakeInputService { Position = new Point(10, 20) };
        private readonly FakeAreaPointResolver _resolver = new FakeAreaPointResolver();
        private readonly FakeWindowService _windows = new FakeWindowService { Window = new WindowHandle(42) };


        // ================================================================
        // Private methods
        // ================================================================
        private static CancellationToken Ct
        {
            get { return TestContext.Current.CancellationToken; }
        }

        private async Task<ExecutionStep> Cursor(FlowStep step, params FlowStep[] others)
        {
            ExecutionCacheService cache = await WorkerCache.ForAsync([step, .. others]);
            return await new CursorStepWorker(_input, _resolver).ExecuteAsync(step, cache, Ct);
        }

        private async Task<ExecutionStep> Keyboard(FlowStep step, ExecutionCacheService? cache = null)
        {
            return await new KeyboardStepWorker(_input).ExecuteAsync(step, cache ?? await WorkerCache.ForAsync(step), Ct);
        }

        private async Task<ExecutionStep> Window(FlowStep step)
        {
            return await new WindowStepWorker(_resolver, _windows).ExecuteAsync(step, await WorkerCache.ForAsync(step), Ct);
        }


        // ================================================================
        // Cursor
        // ================================================================

        [Fact]
        public async Task A_move_goes_to_a_saved_point()
        {
            _resolver.Points[5] = PointResolution.Ok(new Point(200, 80));

            ExecutionStep result = await Cursor(new FlowStep { Id = 1, FlowStepType = FlowStepTypeEnum.CURSOR_RELOCATE, FlowPointId = 5 });

            _input.Actions.ShouldBe(["move 200,80"]);
            result.Location.ShouldBe(new Point(200, 80));
        }

        [Fact]
        public async Task A_move_goes_to_where_an_earlier_search_found_something()
        {
            FlowStep search = new FlowStep { Id = 2, Name = "Find", FlowStepType = FlowStepTypeEnum.SEARCH_IMAGE };
            FlowStep move = new FlowStep { Id = 1, FlowStepType = FlowStepTypeEnum.CURSOR_RELOCATE, FlowStepReferenceId = 2 };
            ExecutionCacheService cache = await WorkerCache.ForAsync(move, search);
            cache.Ran(search, location: new Point(300, 400));

            await new CursorStepWorker(_input, _resolver).ExecuteAsync(move, cache, Ct);

            _input.Actions.ShouldBe(["move 300,400"]);
        }

        [Fact]
        public async Task A_move_with_nowhere_to_go_fails_and_does_nothing()
        {
            ExecutionStep result = await Cursor(new FlowStep { Id = 1, FlowStepType = FlowStepTypeEnum.CURSOR_RELOCATE, FlowPointId = 5 });

            result.Outcome.ShouldBe(StepOutcomeEnum.FAILURE);
            _input.Actions.ShouldBeEmpty();
        }

        [Fact]
        public async Task A_cursor_that_will_not_move_is_a_failure_not_a_silent_pass()
        {
            _resolver.Points[5] = PointResolution.Ok(new Point(200, 80));
            _input.CursorMoves = false;

            ExecutionStep result = await Cursor(new FlowStep { Id = 1, FlowStepType = FlowStepTypeEnum.CURSOR_RELOCATE, FlowPointId = 5 });

            result.Outcome.ShouldBe(StepOutcomeEnum.FAILURE);
        }

        [Theory]
        [InlineData(null, "click 10,20 LEFT_BUTTON")]
        [InlineData(CursorButtonActionTypeEnum.DOUBLE_CLICK, "double click 10,20 LEFT_BUTTON")]
        [InlineData(CursorButtonActionTypeEnum.HOLD_CLICK, "down 10,20 LEFT_BUTTON")]
        [InlineData(CursorButtonActionTypeEnum.RELEASE_CLICK, "up 10,20 LEFT_BUTTON")]
        public async Task A_click_happens_where_the_cursor_already_is(CursorButtonActionTypeEnum? action, string done)
        {
            await Cursor(new FlowStep { Id = 1, FlowStepType = FlowStepTypeEnum.CURSOR_CLICK, CursorButtonActionType = action });

            _input.Actions.ShouldBe([done]);
        }

        [Fact]
        public async Task A_drag_goes_from_one_point_to_the_other()
        {
            _resolver.Points[5] = PointResolution.Ok(new Point(1, 2));
            _resolver.Points[6] = PointResolution.Ok(new Point(3, 4));

            await Cursor(new FlowStep { Id = 1, FlowStepType = FlowStepTypeEnum.CURSOR_DRAG, FlowPointId = 5, FlowPointEndId = 6 });

            _input.Actions.ShouldBe(["move 1,2", "drag 1,2 to 3,4 LEFT_BUTTON"]);
        }

        [Theory]
        [InlineData(CursorScrollDirectionTypeEnum.DOWN, "scroll 10,20 -3")]
        [InlineData(CursorScrollDirectionTypeEnum.UP, "scroll 10,20 3")]
        public async Task A_scroll_turns_the_wheel_its_count_in_its_direction(CursorScrollDirectionTypeEnum direction, string done)
        {
            await Cursor(new FlowStep { Id = 1, FlowStepType = FlowStepTypeEnum.CURSOR_SCROLL, CursorScrollDirectionType = direction, LoopCount = 3 });

            _input.Actions.ShouldBe([done]);
        }

        // Known gap, in TODO.md: the input port has no horizontal wheel, so left and right scroll up.
        [Theory]
        [InlineData(CursorScrollDirectionTypeEnum.LEFT)]
        [InlineData(CursorScrollDirectionTypeEnum.RIGHT)]
        public async Task A_sideways_scroll_turns_the_vertical_wheel_today(CursorScrollDirectionTypeEnum direction)
        {
            await Cursor(new FlowStep { Id = 1, FlowStepType = FlowStepTypeEnum.CURSOR_SCROLL, CursorScrollDirectionType = direction, LoopCount = 3 });

            _input.Actions.ShouldBe(["scroll 10,20 3"]);
        }

        // ================================================================
        // Keyboard
        // ================================================================

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

        // ================================================================
        // System action
        // ================================================================

        [Fact]
        public async Task A_system_action_is_handed_to_the_machine()
        {
            FakeSystemActionService system = new FakeSystemActionService();
            FlowStep step = new FlowStep { Id = 1, FlowStepType = FlowStepTypeEnum.SYSTEM_ACTION, SystemActionType = SystemActionTypeEnum.LOCK_WORKSTATION };

            await new SystemActionStepWorker(system).ExecuteAsync(step, await WorkerCache.ForAsync(step), Ct);

            system.Ran.ShouldBe([SystemActionTypeEnum.LOCK_WORKSTATION]);
        }

        // ================================================================
        // Window
        // ================================================================

        [Fact]
        public async Task A_window_is_found_by_process_and_title_and_focused()
        {
            ExecutionStep result = await Window(new FlowStep { Id = 1, FlowStepType = FlowStepTypeEnum.WINDOW_FOCUS, ProcessName = "chrome", TitlePattern = "Swag", TitleMatchMode = TitleMatchModeEnum.STARTS_WITH });

            _windows.Queries.Single().ShouldSatisfyAllConditions(
                x => x.ProcessName.ShouldBe("chrome"),
                x => x.TitlePattern.ShouldBe("Swag"),
                x => x.TitleMatchMode.ShouldBe(TitleMatchModeEnum.STARTS_WITH));
            _windows.Actions.ShouldBe(["focus"]);
            result.Message.ShouldBe("chrome - focused");
        }

        [Fact]
        public async Task A_window_is_resized_to_its_size()
        {
            await Window(new FlowStep { Id = 1, FlowStepType = FlowStepTypeEnum.WINDOW_RESIZE, ProcessName = "chrome", WindowWidth = 1280, WindowHeight = 720 });

            _windows.Actions.ShouldBe(["resize 1280x720"]);
        }

        [Fact]
        public async Task A_window_is_moved_to_a_point()
        {
            _resolver.Points[5] = PointResolution.Ok(new Point(0, 0));

            await Window(new FlowStep { Id = 1, FlowStepType = FlowStepTypeEnum.WINDOW_RELOCATE, ProcessName = "chrome", FlowPointId = 5 });

            _windows.Actions.ShouldBe(["move 0,0"]);
        }

        [Fact]
        public async Task No_window_is_a_failure_that_names_what_was_looked_for()
        {
            _windows.Window = WindowHandle.None;

            ExecutionStep result = await Window(new FlowStep { Id = 1, FlowStepType = FlowStepTypeEnum.WINDOW_FOCUS, TitlePattern = "Swag", TitleMatchMode = TitleMatchModeEnum.CONTAINS });

            result.Outcome.ShouldBe(StepOutcomeEnum.FAILURE);
            result.Message.ShouldBe(@"Title CONTAINS ""Swag"" - no window matched");
        }

        [Fact]
        public async Task A_window_that_refuses_is_a_failure()
        {
            _windows.Obeys = false;

            ExecutionStep result = await Window(new FlowStep { Id = 1, FlowStepType = FlowStepTypeEnum.WINDOW_RESIZE, ProcessName = "chrome", WindowWidth = 1280, WindowHeight = 720 });

            result.Message.ShouldBe("chrome - the window would not resize");
        }
    }
}
