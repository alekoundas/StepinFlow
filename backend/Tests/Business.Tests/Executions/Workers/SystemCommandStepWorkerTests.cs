using Business.Executions;
using Business.Executions.Workers;
using Business.Tests.Fakes;
using Core.Enums;
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
        private readonly FakeCommandRunner _runner = new FakeCommandRunner();

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

        private async Task<ExecutionStep> Run(FlowStep step, ExecutionCacheService? cache = null)
        {
            return await new SystemCommandStepWorker(_runner).ExecuteAsync(step, cache ?? await WorkerCache.ForAsync(step), Ct);
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
    }
}
