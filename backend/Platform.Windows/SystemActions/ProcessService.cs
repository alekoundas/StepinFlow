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
        // Each owns the windows of programs other than itself: File Explorer's, and the store apps'.
        private static readonly string[] SharedHosts = ["explorer", "ApplicationFrameHost"];

        public bool Kill(int processId)
        {
            try
            {
                using Process process = Process.GetProcessById(processId);

                if (SharedHosts.Contains(process.ProcessName, StringComparer.OrdinalIgnoreCase))
                    return false;

                process.Kill(entireProcessTree: true);
                return true;
            }
            catch (ArgumentException)
            {
                // Not running, so already gone.
                return true;
            }
            catch (Exception)
            {
                return false;
            }
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
