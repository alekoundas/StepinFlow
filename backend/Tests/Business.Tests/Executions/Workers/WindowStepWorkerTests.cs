using System.Drawing;

using Business.AreaPoint;
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
    /// Focusing, resizing, moving and closing a window. One worker for all four, and the window is the
    /// step's area, so the query is built from the area: the message names the area, because "no
    /// window matched" without saying which is a failure nobody can act on.
    /// </summary>
    public sealed class WindowStepWorkerTests
    {
        private readonly FakeAreaPointResolver _resolver = new FakeAreaPointResolver();
        private readonly FakeWindowService _windows = new FakeWindowService { Window = new WindowHandle(42) };
        private readonly FakeBrowserTabService _tabs = new FakeBrowserTabService();
        private readonly FakeProcessService _processes = new FakeProcessService();
        private readonly FakeInputSimulatorService _input = new FakeInputSimulatorService();

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
            BrowserTabFinder tabs = new BrowserTabFinder(_windows, _tabs, TimeProvider.System);
            WindowStepWorker worker = new WindowStepWorker(_resolver, _windows, tabs, _processes, _input, TimeProvider.System);
            return await worker.ExecuteAsync(step, await WorkerCache.ForAsync(step), Ct);
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
                x => x.IgnoreWindowBorders.ShouldBeFalse());
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

        // ================================================================
        // Closing
        // ================================================================

        // Short, so a window that stays open fails without the test waiting the five seconds.
        private static FlowStep Close(FlowArea area, bool isForced = false)
        {
            FlowStep close = Step(FlowStepTypeEnum.WINDOW_CLOSE, area);
            close.TimeoutMilliseconds = 200;
            close.IsCloseForced = isForced;
            return close;
        }

        [Fact]
        public async Task A_window_is_asked_to_close_and_waited_for()
        {
            ExecutionStep result = await Window(Close(Browser));

            _windows.Actions.ShouldBe(["close"]);
            (result.Outcome, result.Message).ShouldBe((StepOutcomeEnum.SUCCESS, "Browser - closed"));
            _processes.Killed.ShouldBeEmpty();
        }

        [Fact]
        public async Task A_window_that_stays_open_is_a_failure_and_its_program_is_left_alone()
        {
            _windows.ClosesWhenAsked = false;

            ExecutionStep result = await Window(Close(Browser));

            (result.Outcome, result.Message).ShouldBe((StepOutcomeEnum.FAILURE, "Browser - still open after 0.2s, often asking whether to save"));
            _processes.Killed.ShouldBeEmpty();
        }

        // By the window's own process, not every process of its name the way Kill a process does.
        [Fact]
        public async Task A_forced_close_ends_the_program_of_that_window()
        {
            _windows.ClosesWhenAsked = false;
            _windows.WindowProcessId = 7140;
            _processes.OnKill = () => _windows.IsClosed = true;

            ExecutionStep result = await Window(Close(Browser, isForced: true));

            _processes.Killed.ShouldBe([7140]);
            (result.Outcome, result.Message).ShouldBe((StepOutcomeEnum.SUCCESS, "Browser - still open after 0.2s, often asking whether to save, so its program was ended"));
        }

        [Fact]
        public async Task A_forced_close_whose_program_will_not_end_is_a_failure()
        {
            _windows.ClosesWhenAsked = false;
            _windows.WindowProcessId = 7140;
            _processes.Ends = false;

            ExecutionStep result = await Window(Close(Browser, isForced: true));

            (result.Outcome, result.Message).ShouldBe((StepOutcomeEnum.FAILURE, "Browser - still open after 0.2s, often asking whether to save, and its program would not end"));
        }

        [Fact]
        public async Task A_tab_is_closed_with_ctrl_w_once_its_browser_is_in_front()
        {
            _windows.Windows.Add(new WindowHandle(1));
            _tabs.With(new WindowHandle(1), 1, "news.example.com", "shop.example.com/cart");
            _input.OnCombination = () => _tabs.CloseFront(new WindowHandle(1));

            ExecutionStep result = await Window(Close(Cart));

            _windows.Actions.ShouldBe(["focus"]);
            _input.Actions.ShouldBe(["press LeftCtrl+W"]);
            (result.Outcome, result.Message).ShouldBe((StepOutcomeEnum.SUCCESS, "Cart - tab closed"));
            _tabs.FrontAddress(new WindowHandle(1)).ShouldBe("news.example.com");
        }

        // Ctrl+W would close whatever is in front instead.
        [Fact]
        public async Task A_tab_whose_browser_will_not_come_to_the_front_is_left_open_and_no_key_is_pressed()
        {
            _windows.Obeys = false;
            _windows.Windows.Add(new WindowHandle(1));
            _tabs.With(new WindowHandle(1), 0, "shop.example.com/cart");

            ExecutionStep result = await Window(Close(Cart));

            _input.Actions.ShouldBeEmpty();
            (result.Outcome, result.Message).ShouldBe((StepOutcomeEnum.FAILURE, "Cart - the browser would not come to the front, so its tab was left open"));
        }

        [Fact]
        public async Task A_tab_that_stays_open_is_a_failure_and_never_forced()
        {
            _windows.Windows.Add(new WindowHandle(1));
            _tabs.With(new WindowHandle(1), 0, "shop.example.com/cart");
            _windows.WindowProcessId = 7140;

            ExecutionStep result = await Window(Close(Cart, isForced: true));

            (result.Outcome, result.Message).ShouldBe((StepOutcomeEnum.FAILURE, "Cart - the tab was still open after 0.2s, often asking whether to leave the page"));
            _processes.Killed.ShouldBeEmpty();
        }

        // ================================================================
        // Browser tabs
        // ================================================================

        private static readonly FlowArea Cart = new FlowArea
        {
            Id = 6,
            Name = "Cart",
            Type = FlowAreaTypeEnum.BROWSER_TAB,
            ProcessName = "chrome",
            TabAddress = "shop.example.com/cart",
        };

        // A browser's title is its front tab's, so every window of the browser is looked in.
        [Fact]
        public async Task A_tab_behind_another_is_brought_to_the_front_of_its_window_and_the_window_focused()
        {
            _windows.Windows.AddRange([new WindowHandle(1), new WindowHandle(2)]);
            _tabs.With(new WindowHandle(1), 0, "news.example.com", "mail.example.com")
                .With(new WindowHandle(2), 0, "docs.example.com", "shop.example.com/cart");

            ExecutionStep result = await Window(Step(FlowStepTypeEnum.WINDOW_FOCUS, Cart));

            result.Outcome.ShouldBe(StepOutcomeEnum.SUCCESS);
            _windows.Queries.Single().ShouldSatisfyAllConditions(
                x => x.ProcessName.ShouldBe("chrome"),
                x => x.TitlePattern.ShouldBe(""));
            _tabs.Selected.ShouldBe(["1:1", "1:0", "2:1"]);
            _tabs.FrontAddress(new WindowHandle(1)).ShouldBe("news.example.com");
            _windows.Actions.ShouldBe(["focus"]);
        }

        [Fact]
        public async Task A_tab_already_in_front_is_left_where_it_is()
        {
            _windows.Windows.Add(new WindowHandle(1));
            _tabs.With(new WindowHandle(1), 1, "news.example.com", "shop.example.com/cart?item=4");

            await Window(Step(FlowStepTypeEnum.WINDOW_FOCUS, Cart));

            _tabs.Selected.ShouldBeEmpty();
            _windows.Actions.ShouldBe(["focus"]);
        }

        [Fact]
        public async Task No_tab_at_the_address_is_a_failure_that_says_so()
        {
            _windows.Windows.Add(new WindowHandle(1));
            _tabs.With(new WindowHandle(1), 0, "news.example.com");

            ExecutionStep result = await Window(Step(FlowStepTypeEnum.WINDOW_FOCUS, Cart));

            (result.Outcome, result.Message).ShouldBe((StepOutcomeEnum.FAILURE, "Cart - no chrome tab is at shop.example.com/cart"));
            _windows.Actions.ShouldBeEmpty();
        }
    }
}
