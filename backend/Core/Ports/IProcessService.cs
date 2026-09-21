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
    }
}
