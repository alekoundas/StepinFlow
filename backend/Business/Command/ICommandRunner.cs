using Core.Models.Dtos;

namespace Business.Services.CommandService
{
    public interface ICommandRunner
    {
        /// <summary>
        /// Runs the step's command and waits for it. Never throws for a command that fails: a
        /// non zero exit or a timeout comes back in the result.
        /// </summary>
        Task<RunCommandTestResultDto> RunAsync(FlowStepDto step, CancellationToken ct = default);
    }
}
