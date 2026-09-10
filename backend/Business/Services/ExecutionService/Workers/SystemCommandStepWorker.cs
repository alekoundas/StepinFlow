
using AutoMapper;
using Business.Services.CommandService;
using Core.Helpers;
using Core.Models.Business;
using Core.Models.Database;
using Core.Models.Dtos;

namespace Business.Services.ExecutionService.Workers
{
    /// <summary>
    /// Runs the step's command. A non zero exit is a Failure result rather than an error
    /// </summary>
    public class SystemCommandStepWorker : IStepWorker
    {
        private readonly ICommandRunner _commandRunner;
        private readonly IMapper _mapper;

        public SystemCommandStepWorker(ICommandRunner commandRunner, IMapper mapper)
        {
            _commandRunner = commandRunner;
            _mapper = mapper;
        }

        public async Task<ExecutionStep> ExecuteAsync(FlowStep step, IExecutionCacheService cache, CancellationToken ct)
        {
            FlowStepDto dto = _mapper.Map<FlowStepDto>(step);

            // A launch line carries the viewport - "--window-size={{width}},{{height}}" - so this is
            // where a flow sizes the application it is testing. The dto is a copy, so resolving into
            // it cannot change the step the cache holds.
            VariableTranslationResult command = cache.TranslateVariables(dto.RunCommandValue);
            if (!command.IsTranslated)
                return ExecutionStep.Failure(VariableTranslator.DescribeUntranslated(command.Untranslated));

            dto.RunCommandValue = command.Text;

            RunCommandTestResultDto run = await _commandRunner.RunAsync(dto, ct);

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
