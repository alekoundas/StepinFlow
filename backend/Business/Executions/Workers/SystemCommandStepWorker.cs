using System.Globalization;

using Business.AreaPoint;
using Business.Shell;
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
    /// Executes the step's command. A non zero exit is a Failure result rather than an error
    ///
    /// A Launch is started rather than executed: it returns once the program is up instead of waiting
    /// for it to close. With an area, it is the window the program opens - left alone when it is
    /// open already, and waited for when it is not, so the next step does not act on a screen that
    /// is still loading. What starts is then the area's Started by, the step's arguments after it.
    ///
    /// A browser tab is open already when it is in front of a window of its browser, and brought
    /// to the front when it is behind; when it is nowhere, its browser opens a new window at its
    /// address.
    /// </summary>
    public class SystemCommandStepWorker : IStepWorker
    {
        // Long enough for an application that shows a splash screen first. A step's own timeout wins.
        private static readonly TimeSpan LAUNCH_TIMEOUT = TimeSpan.FromSeconds(30);
        private static readonly TimeSpan LAUNCH_POLL_INTERVAL = TimeSpan.FromMilliseconds(250);

        private readonly IShellCommandExecutor _shellCommandExecutor;
        private readonly IProcessService _processService;
        private readonly IAreaPointResolver _areaPointResolver;
        private readonly BrowserTabFinder _browserTabFinder;
        private readonly TimeProvider _timeProvider;

        public SystemCommandStepWorker(
            IShellCommandExecutor shellCommandExecutor,
            IProcessService processService,
            IAreaPointResolver areaPointResolver,
            BrowserTabFinder browserTabFinder,
            TimeProvider timeProvider)
        {
            _shellCommandExecutor = shellCommandExecutor;
            _processService = processService;
            _areaPointResolver = areaPointResolver;
            _browserTabFinder = browserTabFinder;
            _timeProvider = timeProvider;
        }

        public async Task<ExecutionStep> ExecuteAsync(FlowStep step, IExecutionCacheService cache, CancellationToken ct)
        {
            // A launch's arguments carry the viewport - "--window-size={{width}},{{height}}" - so this
            // is where a flow sizes the application it is testing. A launch of an area starts what
            // the area is started by, its own arguments after.
            string commandLine = step.ShellCommandValue;
            if (step.ShellCommandMode == ShellCommandModeEnum.LAUNCH_APP && step.FlowArea != null)
                commandLine = $"{step.FlowArea.LaunchCommand} {step.ShellArguments}".Trim();

            // Chromium's browsers and Firefox all take --new-window, so the tab does not land among
            // whatever the last window had open.
            if (step.ShellCommandMode == ShellCommandModeEnum.LAUNCH_APP && step.FlowArea?.Type == FlowAreaTypeEnum.BROWSER_TAB)
                commandLine = $"{commandLine} --new-window {TabAddressHelper.ToOpen(step.FlowArea.TabAddress)}";

            VariableTranslationResult command = cache.TranslateVariables(commandLine);
            if (!command.IsTranslated)
                return ExecutionStep.Failure(VariableTranslator.DescribeUntranslated(command.Untranslated));

            if (step.ShellCommandMode == ShellCommandModeEnum.LAUNCH_APP)
                return await LaunchAsync(step, command.Text, ct);

            // The request is a copy, so resolving into it cannot change the step the cache holds.
            ShellCommandRequest request = ShellCommandRequest.From(step, command.Text);

            ShellCommandResultDto commandResult = await _shellCommandExecutor.ExecuteAsync(request, ct);

            ExecutionStep result = ExecutionStep.Success();

            if (!commandResult.IsSuccess)
                result = ExecutionStep.Failure($"Exited with {commandResult.ExitCode}, expected {step.ShellSuccessExitCodes}.");

            result.Value = commandResult.ResultValue;
            result.ExitCode = commandResult.ExitCode;
            result.Error = commandResult.StandardError;
            result.Command = commandResult.ResolvedCommand;

            return result;
        }


        // ================================================================
        // Private methods
        // ================================================================

        private async Task<ExecutionStep> LaunchAsync(FlowStep step, string commandLine, CancellationToken ct)
        {
            if (step.FlowArea?.Type == FlowAreaTypeEnum.BROWSER_TAB)
            {
                (WindowHandle window, bool wasBehind) = await _browserTabFinder.BringToFrontAsync(step.FlowArea, ct);

                if (window.IsValid && wasBehind)
                    return Launched(ExecutionStep.Success(message: "Open behind another tab, so it was brought to the front rather than launched again."), commandLine);

                if (window.IsValid)
                    return Launched(ExecutionStep.Success(message: "Already open, so not launched again."), commandLine);
            }
            else if (step.FlowAreaId != null)
            {
                AreaResolution open = await _areaPointResolver.ResolveAreaAsync(step.FlowAreaId.Value, ct);

                // A monitor or a region is always there, so it would never be launched.
                if (open.IsResolved && !open.Window.IsValid)
                    return Launched(ExecutionStep.Failure("The launch's area is not a window, so there is nothing to wait for. Pick an application or a browser tab."), commandLine);

                if (open.IsResolved)
                    return Launched(ExecutionStep.Success(message: "Already open, so not launched again."), commandLine);
            }

            string program = CommandLineHelper.SplitProgram(commandLine, out string arguments);

            string? notStarted = _processService.Launch(program, arguments, step.ShellWorkingDirectory);
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
