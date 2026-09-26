using Core.Models.Dtos;

namespace Business.Command
{
    public interface ICommandRunner
    {
        /// <summary>
        /// Runs the command and waits for it. Never throws for a command that fails: a
        /// non zero exit or a timeout comes back in the result.
        /// </summary>
        Task<RunCommandTestResultDto> RunAsync(CommandRequest request, CancellationToken ct = default);
    }
}
