using Core.Ports;

namespace Business.Tests.Fakes
{
    /// <summary>The program files of running processes, by id. Anything else has exited or will not say.</summary>
    public sealed class FakeProcessService : IProcessService
    {
        public Dictionary<int, string> ExecutablePaths { get; } = new Dictionary<int, string>();

        public bool KillByName(string processName)
        {
            return true;
        }

        public string? GetExecutablePath(int processId)
        {
            return ExecutablePaths.GetValueOrDefault(processId);
        }
    }
}
