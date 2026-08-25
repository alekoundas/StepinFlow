namespace Core.Enums
{
    public enum AppSettingKeyEnum
    {
        /// <summary>Width of the screenshot the recorder takes around the cursor.</summary>
        RECORDING_CAPTURE_WIDTH,

        /// <summary>Height of the screenshot the recorder takes around the cursor.</summary>
        RECORDING_CAPTURE_HEIGHT,


        // Debugger hotkeys. Global rather than in-window: while a flow runs, the focused
        // application is the one being automated, so a key handler in our own window never fires.
        HOTKEY_CONTINUE,
        HOTKEY_STEP_INTO,
        HOTKEY_STEP_OVER,
        HOTKEY_PAUSE,
        HOTKEY_STOP,
    }
}
