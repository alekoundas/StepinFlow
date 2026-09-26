using Business.Command;
using Core.Models.Dtos;

namespace Business.Tests.Fakes
{
    /// <summary>
    /// Answers with whatever the test set up, and keeps every request so a test can read what the
    /// worker asked for. Nothing is launched: the real runner starting cmd.exe is the part that
    /// needs a machine, and it is on the other side of the port for exactly this reason.
    /// </summary>
    public sealed class FakeCommandRunner : ICommandRunner
    {
        private RunCommandTestResultDto _answer = new RunCommandTestResultDto { IsSuccess = true };

        public List<CommandRequest> Requests { get; } = new List<CommandRequest>();

        public FakeCommandRunner Answers(RunCommandTestResultDto result)
        {
            _answer = result;
            return this;
        }

        public Task<RunCommandTestResultDto> RunAsync(CommandRequest request, CancellationToken ct = default)
        {
            Requests.Add(request);
            return Task.FromResult(_answer);
        }
    }
}
