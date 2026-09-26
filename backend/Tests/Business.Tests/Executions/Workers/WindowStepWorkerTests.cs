using System.Drawing;

using Business.Executions.Workers;
using Business.Tests.Fakes;
using Core.Enums;
using Core.Models.Business;
using Core.Models.Database;
using static Business.Tests.TestToken;

namespace Business.Tests.Executions.Workers
{
    /// <summary>
    /// Focusing, resizing and moving a window. One worker for all three, so the query it builds is
    /// as much the subject as the action: the message has to name what was looked for, because
    /// "no window matched" without the pattern is a failure nobody can act on.
    /// </summary>
    public sealed class WindowStepWorkerTests
    {
        private readonly FakeAreaPointResolver _resolver = new FakeAreaPointResolver();
        private readonly FakeWindowService _windows = new FakeWindowService { Window = new WindowHandle(42) };

        private async Task<ExecutionStep> Window(FlowStep step)
        {
            return await new WindowStepWorker(_resolver, _windows).ExecuteAsync(step, await WorkerCache.ForAsync(step), Ct);
        }

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
