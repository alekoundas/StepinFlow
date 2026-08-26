
using AutoMapper;
using Business.Services.CommandService;
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
