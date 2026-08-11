namespace Core.Enums
{
    /// <summary>
    /// A ready made command. The step stores the preset and its one parameter rather than the
    /// rendered text, so improving a preset later fixes every flow already using it.
    /// </summary>
    public enum RunCommandPresetEnum
    {
        CUSTOM,
        KILL_PROCESS,
        IS_PROCESS_RUNNING,
        READ_CLIPBOARD,
        WRITE_CLIPBOARD,
        CHECK_INTERNET,
        SHUTDOWN_IN,
        CANCEL_SHUTDOWN,
    }
}
