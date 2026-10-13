using System.Globalization;

using Business.AreaPoint;
using Business.Command;
using Core.Enums;
using Core.Helpers;
using Core.Models.Business;
using Core.Models.Business.AreaPointResolution;
using Core.Models.Database;
using Core.Models.Dtos;
using Core.Ports;

namespace Business.Executions.Workers
{
    /// <summary>
    /// Runs the step's command. A non zero exit is a Failure result rather than an error
    ///
    /// A Launch is started rather than run: it returns once the program is up instead of waiting
    /// for it to close. With an area, it is the window the program opens - left alone when it is
    /// open already, and waited for when it is not, so the next step does not act on a screen that
    /// is still loading.
    /// </summary>
    public class SystemCommandStepWorker : IStepWorker
    {
        // Long enough for an application that shows a splash screen first. A step's own timeout wins.
        private static readonly TimeSpan LAUNCH_TIMEOUT = TimeSpan.FromSeconds(30);
        private static readonly TimeSpan LAUNCH_POLL_INTERVAL = TimeSpan.FromMilliseconds(250);

        private readonly ICommandRunner _commandRunner;
        private readonly IProcessService _processService;
        private readonly IAreaPointResolver _areaPointResolver;
        private readonly TimeProvider _timeProvider;

        public SystemCommandStepWorker(
            ICommandRunner commandRunner,
            IProcessService processService,
            IAreaPointResolver areaPointResolver,
            TimeProvider timeProvider)
        {
            _commandRunner = commandRunner;
            _processService = processService;
            _areaPointResolver = areaPointResolver;
            _timeProvider = timeProvider;
        }

        public async Task<ExecutionStep> ExecuteAsync(FlowStep step, IExecutionCacheService cache, CancellationToken ct)
        {
            // A launch line carries the viewport - "--window-size={{width}},{{height}}" - so this is
            // where a flow sizes the application it is testing.
            VariableTranslationResult command = cache.TranslateVariables(step.RunCommandValue);
            if (!command.IsTranslated)
                return ExecutionStep.Failure(VariableTranslator.DescribeUntranslated(command.Untranslated));

            if (step.RunCommandPreset == RunCommandPresetEnum.LAUNCH_APP)
                return await LaunchAsync(step, command.Text, ct);

            // The request is a copy, so resolving into it cannot change the step the cache holds.
            CommandRequest request = CommandRequest.From(step, command.Text);

            RunCommandTestResultDto run = await _commandRunner.RunAsync(request, ct);

            ExecutionStep result = ExecutionStep.Success();

            if (!run.IsSuccess)
                result = ExecutionStep.Failure($"Exited with {run.ExitCode}, expected {step.SuccessExitCodes}.");

            result.Value = run.ResultValue;
            result.ExitCode = run.ExitCode;
            result.Error = run.StandardError;
            result.Command = run.ResolvedCommand;

            return result;
        }


        // ================================================================
        // Private methods
        // ================================================================

        private async Task<ExecutionStep> LaunchAsync(FlowStep step, string commandLine, CancellationToken ct)
        {
            if (step.FlowAreaId != null)
            {
                AreaResolution open = await _areaPointResolver.ResolveAreaAsync(step.FlowAreaId.Value, ct);

                // A monitor or a region is always there, so it would never be launched.
                if (open.IsResolved && !open.Window.IsValid)
                    return Launched(ExecutionStep.Failure("The launch's area is not a window, so there is nothing to wait for. Pick an application or a browser tab."), commandLine);

                if (open.IsResolved)
                    return Launched(ExecutionStep.Success(message: "Already open, so not launched again."), commandLine);
            }

            string program = CommandLineHelper.SplitProgram(commandLine, out string arguments);

            string? notStarted = _processService.Launch(program, arguments, step.RunCommandWorkingDirectory);
            if (notStarted != null)
                return Launched(ExecutionStep.Failure($"Could not launch {program}: {notStarted}"), commandLine);

            if (step.FlowAreaId == null)
                return Launched(ExecutionStep.Success(), commandLine);

            return await WaitForWindowAsync(step, step.FlowAreaId.Value, commandLine, ct);
        }

        private async Task<ExecutionStep> WaitForWindowAsync(FlowStep step, int flowAreaId, string commandLine, CancellationToken ct)
        {
            TimeSpan timeout = LAUNCH_TIMEOUT;
            if (step.TimeoutMilliseconds > 0)
                timeout = TimeSpan.FromMilliseconds(step.TimeoutMilliseconds);

            DateTimeOffset giveUpAt = _timeProvider.GetUtcNow() + timeout;

            while (true)
            {
                AreaResolution area = await _areaPointResolver.ResolveAreaAsync(flowAreaId, ct);
                if (area.IsResolved)
                    return Launched(ExecutionStep.Success(), commandLine);

                if (_timeProvider.GetUtcNow() >= giveUpAt)
                {
                    string seconds = timeout.TotalSeconds.ToString("0.#", CultureInfo.InvariantCulture);
                    return Launched(ExecutionStep.Failure($"Launched, but its window did not appear within {seconds}s. {area.Error}"), commandLine);
                }

                await Task.Delay(LAUNCH_POLL_INTERVAL, _timeProvider, ct);
            }
        }

        private static ExecutionStep Launched(ExecutionStep result, string commandLine)
        {
            result.Command = commandLine;
            return result;
        }
    }
}
