using System.Drawing;

using Business.Executions.Workers;
using Business.Tests.Fakes;
using Core.Enums;
using Core.Models.Business;
using Core.Models.Business.AreaPointResolution;
using Core.Models.Database;
using static Business.Tests.TestToken;

namespace Business.Tests.Executions.Workers
{
    /// <summary>
    /// Focusing, resizing and moving a window. One worker for all three, and the window is the step's
    /// area, so the query is built from the area: the message names the area, because "no window
    /// matched" without saying which is a failure nobody can act on.
    /// </summary>
    public sealed class WindowStepWorkerTests
    {
        private readonly FakeAreaPointResolver _resolver = new FakeAreaPointResolver();
        private readonly FakeWindowService _windows = new FakeWindowService { Window = new WindowHandle(42) };

        private static readonly FlowArea Browser = new FlowArea
        {
            Id = 3,
            Name = "Browser",
            Type = FlowAreaTypeEnum.APPLICATION,
            ProcessName = "chrome",
            TitlePattern = "Swag",
            TitleMatchMode = TitleMatchModeEnum.STARTS_WITH,
        };

        private async Task<ExecutionStep> Window(FlowStep step)
        {
            return await new WindowStepWorker(_resolver, _windows).ExecuteAsync(step, await WorkerCache.ForAsync(step), Ct);
        }

        private static FlowStep Step(FlowStepTypeEnum type, FlowArea? area)
        {
            return new FlowStep { Id = 1, FlowStepType = type, FlowArea = area, FlowAreaId = area?.Id };
        }

        [Fact]
        public async Task A_window_is_found_the_way_its_area_finds_it_and_focused()
        {
            ExecutionStep result = await Window(Step(FlowStepTypeEnum.WINDOW_FOCUS, Browser));

            _windows.Queries.Single().ShouldSatisfyAllConditions(
                x => x.ProcessName.ShouldBe("chrome"),
                x => x.TitlePattern.ShouldBe("Swag"),
                x => x.TitleMatchMode.ShouldBe(TitleMatchModeEnum.STARTS_WITH),
                x => x.UseClientArea.ShouldBeFalse());
            _windows.Actions.ShouldBe(["focus"]);
            result.Message.ShouldBe("Browser - focused");
        }

        [Fact]
        public async Task A_window_is_resized_to_its_size()
        {
            FlowStep resize = Step(FlowStepTypeEnum.WINDOW_RESIZE, Browser);
            (resize.WindowWidth, resize.WindowHeight) = (1280, 720);

            await Window(resize);

            _windows.Actions.ShouldBe(["resize 1280x720"]);
        }

        [Fact]
        public async Task A_window_is_moved_to_a_point()
        {
            _resolver.Points[5] = PointResolution.Ok(new Point(0, 0));
            FlowStep move = Step(FlowStepTypeEnum.WINDOW_RELOCATE, Browser);
            move.FlowPointId = 5;

            await Window(move);

            _windows.Actions.ShouldBe(["move 0,0"]);
        }

        [Fact]
        public async Task No_window_is_a_failure_that_names_the_area()
        {
            _windows.Window = WindowHandle.None;

            ExecutionStep result = await Window(Step(FlowStepTypeEnum.WINDOW_FOCUS, Browser));

            result.Outcome.ShouldBe(StepOutcomeEnum.FAILURE);
            result.Message.ShouldBe("Browser - no window matched");
        }

        [Fact]
        public async Task A_window_that_refuses_is_a_failure()
        {
            _windows.Obeys = false;
            FlowStep resize = Step(FlowStepTypeEnum.WINDOW_RESIZE, Browser);
            (resize.WindowWidth, resize.WindowHeight) = (1280, 720);

            ExecutionStep result = await Window(resize);

            result.Message.ShouldBe("Browser - the window would not resize");
        }

        [Fact]
        public async Task A_step_with_no_area_or_an_area_that_is_not_a_window_looks_for_nothing()
        {
            FlowArea monitor = new FlowArea { Id = 4, Name = "Screen", Type = FlowAreaTypeEnum.MONITOR };

            ExecutionStep none = await Window(Step(FlowStepTypeEnum.WINDOW_FOCUS, null));
            ExecutionStep notWindow = await Window(Step(FlowStepTypeEnum.WINDOW_FOCUS, monitor));

            (none.Outcome, none.Message).ShouldBe((StepOutcomeEnum.FAILURE, "There is no window to act on."));
            (notWindow.Outcome, notWindow.Message).ShouldBe((StepOutcomeEnum.FAILURE, "Screen - not a window"));
            _windows.Queries.ShouldBeEmpty();
        }
    }
}
