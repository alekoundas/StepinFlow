namespace Core.Ports
{
    // Dependency Inversion Principle(DIP)
    public interface IProcessService
    {
        /// <summary>
        /// Force closes one process, and everything it started: the one a window belongs to. Never
        /// throws. False means it is still running, or that it holds the windows of other programs
        /// too, the way the desktop's shell does, and was left alone.
        /// </summary>
        bool Kill(int processId);

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
