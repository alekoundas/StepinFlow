using System.Diagnostics;

using Core.Ports;

namespace Platform.Windows.SystemActions
{
    /// <summary>
    /// The process table, which .NET exposes the same way on every OS. It lives here rather than in
    /// Business because ending someone else's program is touching the machine, whoever asks.
    /// </summary>
    public sealed class ProcessService : IProcessService
    {
        public bool KillByName(string processName)
        {
            if (string.IsNullOrWhiteSpace(processName))
                return true;

            // GetProcessesByName wants the name without the extension, and what is stored is
            // whatever the window picker read off the process - "chrome.exe" as often as "chrome".
            string name = Path.GetFileNameWithoutExtension(processName);
            bool allGone = true;

            foreach (Process process in Process.GetProcessesByName(name))
            {
                try
                {
                    process.Kill(entireProcessTree: true);
                }
                catch (Exception)
                {
                    // Already gone, or running as administrator while this is not. Neither is worth
                    // abandoning the rest of the list for.
                    allGone = false;
                }
                finally
                {
                    process.Dispose();
                }
            }

            return allGone;
        }

        public string? Launch(string program, string arguments, string workingDirectory)
        {
            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                FileName = program,
                Arguments = arguments,
                UseShellExecute = true,
            };

            if (!string.IsNullOrWhiteSpace(workingDirectory))
                startInfo.WorkingDirectory = workingDirectory;

            try
            {
                // Null when the shell handed it to an instance already running, which still started it.
                Process.Start(startInfo)?.Dispose();
                return null;
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
        }

        public string? GetExecutablePath(int processId)
        {
            try
            {
                using Process process = Process.GetProcessById(processId);
                return process.MainModule?.FileName;
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}
