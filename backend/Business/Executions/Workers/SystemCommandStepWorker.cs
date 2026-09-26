using Business.Command;
using Core.Helpers;
using Core.Models.Business;
using Core.Models.Database;
using Core.Models.Dtos;

namespace Business.Executions.Workers
{
    /// <summary>
    /// Runs the step's command. A non zero exit is a Failure result rather than an error
    /// </summary>
    public class SystemCommandStepWorker : IStepWorker
    {
        private readonly ICommandRunner _commandRunner;

        public SystemCommandStepWorker(ICommandRunner commandRunner)
        {
            _commandRunner = commandRunner;
        }

        public async Task<ExecutionStep> ExecuteAsync(FlowStep step, IExecutionCacheService cache, CancellationToken ct)
        {
            // A launch line carries the viewport - "--window-size={{width}},{{height}}" - so this is
            // where a flow sizes the application it is testing.
            VariableTranslationResult command = cache.TranslateVariables(step.RunCommandValue);
            if (!command.IsTranslated)
                return ExecutionStep.Failure(VariableTranslator.DescribeUntranslated(command.Untranslated));

            // The request is a copy, so resolving into it cannot change the step the cache holds.
            CommandRequest request = CommandRequest.From(step) with { Value = command.Text };

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
    }
}
