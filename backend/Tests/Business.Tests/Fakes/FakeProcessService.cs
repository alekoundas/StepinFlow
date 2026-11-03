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

        // Each process ended by its id, whether it would end, and what ending it does - close its
        // window, say.
        public List<int> Killed { get; } = new List<int>();
        public bool Ends { get; set; } = true;
        public Action? OnKill { get; set; }

        public bool Kill(int processId)
        {
            if (!Ends)
                return false;

            Killed.Add(processId);
            OnKill?.Invoke();
            return true;
        }

        public string? GetExecutablePath(int processId)
        {
            return ExecutablePaths.GetValueOrDefault(processId);
        }

        // Each launch as "program | arguments", why the next one fails when it should, and what the
        // program does once started - open its window, say.
        public List<string> Launched { get; } = new List<string>();
        public string? LaunchError { get; set; }
        public Action? OnLaunch { get; set; }

        public string? Launch(string program, string arguments, string workingDirectory)
        {
            if (LaunchError != null)
                return LaunchError;

            Launched.Add($"{program} | {arguments}");
            OnLaunch?.Invoke();
            return null;
        }

        public Task WaitForExitAsync(int processId, CancellationToken ct)
        {
            return Task.CompletedTask;
        }
    }
}
