namespace Core.Enums
{
    /// <summary>
    /// Things Windows does through an API call rather than a command. They produce no output and
    /// cannot fail on their own terms, so a SYSTEM_ACTION step is always a leaf.
    /// </summary>
    public enum SystemActionTypeEnum
    {
        LOCK_WORKSTATION,
        SLEEP_PC,
        MONITOR_OFF,
        MONITOR_ON,
    }
}
