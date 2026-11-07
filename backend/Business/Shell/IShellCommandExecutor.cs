using Core.Models.Dtos;

namespace Business.Shell
{
    public interface IShellCommandExecutor
    {
        /// <summary>
        /// Executes the command and waits for it. Never throws for a command that fails: a
        /// non zero exit or a timeout comes back in the result.
        /// </summary>
        Task<ShellCommandResultDto> ExecuteAsync(ShellCommandRequest request, CancellationToken ct = default);
    }
}
