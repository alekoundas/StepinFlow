using Business.Shell;
using Core.Models.Dtos;

namespace Business.Tests.Fakes
{
    /// <summary>
    /// Answers with whatever the test set up, and keeps every request so a test can read what the
    /// worker asked for. Nothing is launched: the real executor starting cmd.exe is the part that
    /// needs a machine, and it is on the other side of the port for exactly this reason.
    /// </summary>
    public sealed class FakeShellCommandExecutor : IShellCommandExecutor
    {
        private ShellCommandResultDto _answer = new ShellCommandResultDto { IsSuccess = true };

        public List<ShellCommandRequest> Requests { get; } = new List<ShellCommandRequest>();

        public FakeShellCommandExecutor Answers(ShellCommandResultDto result)
        {
            _answer = result;
            return this;
        }

        public Task<ShellCommandResultDto> ExecuteAsync(ShellCommandRequest request, CancellationToken ct = default)
        {
            Requests.Add(request);
            return Task.FromResult(_answer);
        }
    }
}
