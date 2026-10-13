namespace Core.Ports
{
    // Dependency Inversion Principle(DIP)
    public interface IProcessService
    {
        /// <summary>
        /// Force closes every process with this name, and everything each of them started. By name
        /// rather than by id: nothing above this port holds one, and an application under test may
        /// have put up more than one instance of itself.
        ///
        /// Never throws. False means at least one is still running, which is usually an elevated
        /// process refusing a caller that is not.
        /// </summary>
        bool KillByName(string processName);

        /// <summary>
        /// The program file the process was started from. Null when it has exited, or will not
        /// say, which is usually an elevated process refusing a caller that is not.
        /// </summary>
        string? GetExecutablePath(int processId);

        /// <summary>
        /// Starts a program, a document or a URL the way the desktop opens one, and returns at once
        /// rather than waiting for it to close. Never throws: null when it started, else why not.
        /// </summary>
        string? Launch(string program, string arguments, string workingDirectory);
    }
}
