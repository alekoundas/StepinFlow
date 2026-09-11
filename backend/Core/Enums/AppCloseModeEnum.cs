namespace Core.Enums
{
    public enum AppCloseModeEnum
    {
        /// <summary>Leave it open. For a flow that did not start the application in the first place.</summary>
        LEAVE,
        CLOSE_WINDOW,
        KILL_PROCESS,
    }
}
