using Business.Shell;
using Core.Models.Dtos;

namespace Transport.Ipc.Handlers
{
    /// <summary>
    /// Executes the step's command for real so the author can see what it returns. There is no way to
    /// preview a command without executing it, which is why destructive modes ask first.
    /// </summary>
    public class TestShellCommandHandler
    {
        private readonly IShellCommandExecutor _shellCommandExecutor;

        public TestShellCommandHandler(IShellCommandExecutor shellCommandExecutor)
        {
            _shellCommandExecutor = shellCommandExecutor;
        }

        public async Task<ResultDto<ShellCommandResultDto>> HandleAsync(FlowStepDto dto, CancellationToken ct)
        {
            ShellCommandResultDto result = await _shellCommandExecutor.ExecuteAsync(ShellCommandRequest.From(dto), ct);
            return ResultDto<ShellCommandResultDto>.Success(result);
        }
    }
}
