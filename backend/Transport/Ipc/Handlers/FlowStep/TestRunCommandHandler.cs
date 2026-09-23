using Business.Services.CommandService;
using Core.Models.Dtos;

namespace Transport.Ipc.Handlers
{
    /// <summary>
    /// Runs the step's command for real so the author can see what it returns. There is no way to
    /// preview a command without running it, which is why destructive presets ask first.
    /// </summary>
    public class TestRunCommandHandler
    {
        private readonly ICommandRunner _commandRunner;

        public TestRunCommandHandler(ICommandRunner commandRunner)
        {
            _commandRunner = commandRunner;
        }

        public async Task<ResultDto<RunCommandTestResultDto>> HandleAsync(FlowStepDto dto, CancellationToken ct)
        {
            RunCommandTestResultDto result = await _commandRunner.RunAsync(dto, ct);
            return ResultDto<RunCommandTestResultDto>.Success(result);
        }
    }
}
