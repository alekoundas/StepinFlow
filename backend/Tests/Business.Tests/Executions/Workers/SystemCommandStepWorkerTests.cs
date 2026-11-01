using System.Drawing;

using Business.AreaPoint;
using Business.Executions;
using Business.Executions.Workers;
using Business.Tests.Fakes;
using Core.Enums;
using Core.Models.Business;
using Core.Models.Business.AreaPointResolution;
using Core.Models.Database;
using Core.Models.Dtos;
using static Business.Tests.TestToken;

namespace Business.Tests.Executions.Workers
{
    /// <summary>
    /// Running a shell command. The worker itself decides three things - what the command says once
    /// its variables are filled in, whether the exit code counts as a pass, and what travels out for
    /// a later step to read - and launching the process is the runner's job behind the port.
    ///
    /// This is also where a flow sizes the application it is testing, because a launch line carries
    /// the viewport as "--window-size={{width}},{{height}}".
    /// </summary>
    public sealed class SystemCommandStepWorkerTests
    {
        private const int APP_WINDOW_AREA = 5;
        private const string APP = @"C:\Program Files\App\app.exe";

        private static readonly AreaResolution OPEN = AreaResolution.Ok(new Rectangle(0, 0, 800, 600), new WindowHandle(7));

        private readonly FakeCommandRunner _runner = new FakeCommandRunner();
        private readonly FakeProcessService _processes = new FakeProcessService();
        private readonly FakeAreaPointResolver _resolver = new FakeAreaPointResolver();
        private readonly FakeWindowService _windows = new FakeWindowService();
        private readonly FakeBrowserTabService _tabs = new FakeBrowserTabService();

        private static FlowStep Command(string value, string successExitCodes = "0")
        {
            return new FlowStep
            {
                Id = 1,
                Name = "Launch",
                FlowStepType = FlowStepTypeEnum.SYSTEM_COMMAND,
                RunCommandValue = value,
                RunCommandShell = RunCommandShellEnum.POWERSHELL,
                RunCommandWorkingDirectory = @"C:\app",
                SuccessExitCodes = successExitCodes,
                TimeoutMilliseconds = 5000,
                ResultSource = ResultSourceEnum.STDERR,
                ResultExtractPattern = @"(\d+)",
            };
        }

        // Of an area, it starts what the area is started by with its arguments after; of nothing,
        // its own command line.
        private static FlowStep Launch(int? flowAreaId = APP_WINDOW_AREA, int timeoutMilliseconds = 0)
        {
            FlowStep step = new FlowStep
            {
                Id = 1,
                Name = "Launch",
                FlowStepType = FlowStepTypeEnum.SYSTEM_COMMAND,
                RunCommandPreset = RunCommandPresetEnum.LAUNCH_APP,
                FlowAreaId = flowAreaId,
                TimeoutMilliseconds = timeoutMilliseconds,
            };

            if (flowAreaId == null)
            {
                step.RunCommandValue = $"\"{APP}\" --profile test";
                return step;
            }

            step.FlowArea = new FlowArea { Id = flowAreaId.Value, Name = "App", LaunchCommand = $"\"{APP}\"" };
            step.LaunchArguments = "--profile test";
            return step;
        }

        private async Task<ExecutionStep> Run(FlowStep step, ExecutionCacheService? cache = null)
        {
            BrowserTabFinder tabs = new BrowserTabFinder(_windows, _tabs, TimeProvider.System);
            SystemCommandStepWorker worker = new SystemCommandStepWorker(_runner, _processes, _resolver, tabs, TimeProvider.System);
            return await worker.ExecuteAsync(step, cache ?? await WorkerCache.ForAsync(step), Ct);
        }

        [Fact]
        public async Task An_exit_code_the_step_expects_is_a_pass()
        {
            _runner.Answers(new RunCommandTestResultDto { IsSuccess = true, ExitCode = 0, ResultValue = "42", ResolvedCommand = "npm start" });

            ExecutionStep result = await Run(Command("npm start"));

            result.Outcome.ShouldBe(StepOutcomeEnum.SUCCESS);
            result.Value.ShouldBe("42");
            result.Command.ShouldBe("npm start");
        }

        // A command that fails is the product being broken, not the harness, so it takes the Failure
        // branch rather than ending the execution.
        [Fact]
        public async Task An_exit_code_it_does_not_expect_fails_saying_both_numbers()
        {
            _runner.Answers(new RunCommandTestResultDto { IsSuccess = false, ExitCode = 3, StandardError = "not found" });

            ExecutionStep result = await Run(Command("npm start", successExitCodes: "0,1"));

            result.Outcome.ShouldBe(StepOutcomeEnum.FAILURE);
            result.Message.ShouldBe("Exited with 3, expected 0,1.");
            result.ExitCode.ShouldBe(3);
            result.Error.ShouldBe("not found");
        }

        [Fact]
        public async Task The_command_is_asked_for_with_its_variables_filled_in()
        {
            FlowStep read = new FlowStep { Id = 2, Name = "port", FlowStepType = FlowStepTypeEnum.SEARCH_TEXT };
            FlowStep command = Command("serve --port {{port}}");
            ExecutionCacheService cache = await WorkerCache.ForAsync(command, read);
            cache.Ran(read, value: "8080");

            await Run(command, cache);

            _runner.Requests.Single().Value.ShouldBe("serve --port 8080");
        }

        // Everything the runner needs travels with the request, so the worker needs no mapper and the
        // runner never sees a flow step.
        [Fact]
        public async Task Everything_else_the_runner_needs_travels_with_it()
        {
            await Run(Command("npm start"));

            _runner.Requests.Single().ShouldSatisfyAllConditions(
                x => x.Shell.ShouldBe(RunCommandShellEnum.POWERSHELL),
                x => x.WorkingDirectory.ShouldBe(@"C:\app"),
                x => x.TimeoutMilliseconds.ShouldBe(5000),
                x => x.SuccessExitCodes.ShouldBe("0"),
                x => x.ResultSource.ShouldBe(ResultSourceEnum.STDERR),
                x => x.ResultExtractPattern.ShouldBe(@"(\d+)"));
        }

        // Running "serve --port {{port}}" literally would start the wrong thing rather than fail.
        [Fact]
        public async Task A_variable_with_no_value_runs_nothing_and_fails_naming_it()
        {
            ExecutionStep result = await Run(Command("serve --port {{port}}"));

            result.Outcome.ShouldBe(StepOutcomeEnum.FAILURE);
            result.Message.ShouldBe("Nothing has a value for {{port}}.");
            _runner.Requests.ShouldBeEmpty();
        }

        // The step it was translated for keeps its variables: the next pass of a loop has to resolve
        // them again, against whatever the flow knows by then.
        [Fact]
        public async Task Filling_the_command_in_does_not_rewrite_the_step()
        {
            FlowStep read = new FlowStep { Id = 2, Name = "port", FlowStepType = FlowStepTypeEnum.SEARCH_TEXT };
            FlowStep command = Command("serve --port {{port}}");
            ExecutionCacheService cache = await WorkerCache.ForAsync(command, read);
            cache.Ran(read, value: "8080");

            await Run(command, cache);

            command.RunCommandValue.ShouldBe("serve --port {{port}}");
        }

        // ================================================================
        // Launch
        // ================================================================

        // A run waits for the program to close, which an application never does on its own.
        [Fact]
        public async Task A_launch_starts_the_program_with_its_arguments_rather_than_running_it_as_a_command()
        {
            ExecutionStep result = await Run(Launch(flowAreaId: null));

            result.Outcome.ShouldBe(StepOutcomeEnum.SUCCESS);
            _processes.Launched.ShouldBe([$"{APP} | --profile test"]);
            _runner.Requests.ShouldBeEmpty();
        }

        [Fact]
        public async Task An_application_already_open_is_not_launched_again()
        {
            _resolver.Areas[APP_WINDOW_AREA] = OPEN;

            ExecutionStep result = await Run(Launch());

            result.Outcome.ShouldBe(StepOutcomeEnum.SUCCESS);
            result.Message.ShouldBe("Already open, so not launched again.");
            _processes.Launched.ShouldBeEmpty();
        }

        // So the next step does not act on a screen that is still loading.
        [Fact]
        public async Task After_launching_it_waits_for_its_window()
        {
            _processes.OnLaunch = () => _resolver.Areas[APP_WINDOW_AREA] = OPEN;

            ExecutionStep result = await Run(Launch());

            result.Outcome.ShouldBe(StepOutcomeEnum.SUCCESS);
            _processes.Launched.ShouldHaveSingleItem();
        }

        [Fact]
        public async Task A_window_that_never_appears_fails_once_the_timeout_runs_out()
        {
            ExecutionStep result = await Run(Launch(timeoutMilliseconds: 1));

            result.Outcome.ShouldBe(StepOutcomeEnum.FAILURE);
            result.Message!.ShouldStartWith("Launched, but its window did not appear within 0s.");
        }

        [Fact]
        public async Task A_program_that_will_not_start_fails_saying_why()
        {
            _processes.LaunchError = "The system cannot find the file specified.";

            ExecutionStep result = await Run(Launch());

            result.Outcome.ShouldBe(StepOutcomeEnum.FAILURE);
            result.Message.ShouldBe($"Could not launch {APP}: The system cannot find the file specified.");
        }

        // A monitor is always there, so it would never be launched.
        [Fact]
        public async Task An_area_that_is_not_a_window_cannot_be_waited_for()
        {
            _resolver.Areas[APP_WINDOW_AREA] = AreaResolution.Ok(new Rectangle(0, 0, 1920, 1080));

            ExecutionStep result = await Run(Launch());

            result.Outcome.ShouldBe(StepOutcomeEnum.FAILURE);
            _processes.Launched.ShouldBeEmpty();
        }

        [Fact]
        public async Task A_launch_of_a_steam_game_starts_its_link()
        {
            FlowStep launch = Launch();
            launch.FlowArea!.LaunchCommand = "steam://rungameid/1013320";
            launch.LaunchArguments = "";

            await Run(launch);

            _processes.Launched.ShouldBe(["steam://rungameid/1013320 | "]);
        }

        // The QA test's fresh profile and window size go after whatever the app is started with.
        [Fact]
        public async Task A_launchs_arguments_go_after_the_ones_its_area_is_started_with()
        {
            FlowStep launch = Launch();
            launch.FlowArea!.LaunchCommand = $"\"{APP}\" --safe-mode";

            await Run(launch);

            _processes.Launched.ShouldBe([$"{APP} | --safe-mode --profile test"]);
        }

        // An earlier step's result is as good in the arguments as in a command line.
        [Fact]
        public async Task A_launchs_arguments_have_their_variables_filled_in()
        {
            FlowStep read = new FlowStep { Id = 2, Name = "port", FlowStepType = FlowStepTypeEnum.SEARCH_TEXT };
            FlowStep launch = Launch(flowAreaId: APP_WINDOW_AREA);
            launch.LaunchArguments = "--port {{port}}";
            ExecutionCacheService cache = await WorkerCache.ForAsync(launch, read);
            cache.Ran(read, value: "8080");
            _processes.OnLaunch = () => _resolver.Areas[APP_WINDOW_AREA] = OPEN;

            await Run(launch, cache);

            _processes.Launched.ShouldBe([$"{APP} | --port 8080"]);
        }

        // ================================================================
        // Launching a browser tab
        // ================================================================

        private static FlowStep LaunchOfTab()
        {
            FlowArea cart = new FlowArea
            {
                Id = APP_WINDOW_AREA,
                Name = "Cart",
                Type = FlowAreaTypeEnum.BROWSER_TAB,
                ProcessName = "chrome",
                TabAddress = "shop.example.com/cart",
                LaunchCommand = "chrome.exe",
            };

            return new FlowStep
            {
                Id = 1,
                Name = "Launch",
                FlowStepType = FlowStepTypeEnum.SYSTEM_COMMAND,
                RunCommandPreset = RunCommandPresetEnum.LAUNCH_APP,
                FlowArea = cart,
                FlowAreaId = cart.Id,
            };
        }

        [Fact]
        public async Task A_tab_in_front_is_not_launched_again()
        {
            _windows.Windows.Add(new WindowHandle(1));
            _tabs.With(new WindowHandle(1), 0, "shop.example.com/cart");

            ExecutionStep result = await Run(LaunchOfTab());

            result.Message.ShouldBe("Already open, so not launched again.");
            _processes.Launched.ShouldBeEmpty();
        }

        [Fact]
        public async Task A_tab_behind_another_is_brought_to_the_front_rather_than_launched()
        {
            _windows.Windows.Add(new WindowHandle(1));
            _tabs.With(new WindowHandle(1), 0, "news.example.com", "shop.example.com/cart");

            ExecutionStep result = await Run(LaunchOfTab());

            result.Outcome.ShouldBe(StepOutcomeEnum.SUCCESS);
            _tabs.FrontAddress(new WindowHandle(1)).ShouldBe("shop.example.com/cart");
            _processes.Launched.ShouldBeEmpty();
        }

        // A new window, so the tab does not land among whatever the browser's last window had open.
        [Fact]
        public async Task A_tab_open_nowhere_opens_its_browser_at_its_address_in_a_new_window()
        {
            _processes.OnLaunch = () => _resolver.Areas[APP_WINDOW_AREA] = OPEN;

            ExecutionStep result = await Run(LaunchOfTab());

            result.Outcome.ShouldBe(StepOutcomeEnum.SUCCESS);
            _processes.Launched.ShouldBe(["chrome.exe | --new-window https://shop.example.com/cart"]);
        }
    }
}
