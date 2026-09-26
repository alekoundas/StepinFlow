using System.Drawing;

using Business.Executions;
using Business.Executions.Workers;
using Business.Tests.Fakes;
using Core.Enums;
using Core.Models.Business;
using Core.Models.Database;
using static Business.Tests.TestToken;

namespace Business.Tests.Executions.Workers
{
    /// <summary>
    /// Clicks, drags, scrolls and moves. The fake input service records what it was told as readable
    /// lines - "move 200,80" - so an assertion reads like the flow rather than like a mock.
    ///
    /// What is worth pinning is that nothing happens when the target cannot be worked out: a cursor
    /// step that quietly clicks the wrong place is worse than one that fails.
    /// </summary>
    public sealed class CursorStepWorkerTests
    {
        private readonly FakeInputService _input = new FakeInputService { Position = new Point(10, 20) };
        private readonly FakeAreaPointResolver _resolver = new FakeAreaPointResolver();

        private async Task<ExecutionStep> Cursor(FlowStep step, params FlowStep[] others)
        {
            ExecutionCacheService cache = await WorkerCache.ForAsync([step, .. others]);
            return await new CursorStepWorker(_input, _resolver).ExecuteAsync(step, cache, Ct);
        }

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
    }
}
