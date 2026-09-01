namespace Core.Enums
{
    public enum AppSettingKeyEnum
    {
        /// <summary>Width of the screenshot the recorder takes around the cursor.</summary>
        RECORDING_CAPTURE_WIDTH,

        /// <summary>Height of the screenshot the recorder takes around the cursor.</summary>
        RECORDING_CAPTURE_HEIGHT,


        // Debugger hotkeys.
        HOTKEY_CONTINUE,
        HOTKEY_STEP_INTO,
        HOTKEY_STEP_OVER,
        HOTKEY_PAUSE,
        HOTKEY_STOP,


        /// <summary>How many screenshots are kept, to be written out when a step fails.</summary>
        EXECUTION_SCREENSHOT_RING_SIZE,


        /// <summary>Who runs the model, and what it needs to reach it.</summary>
        AI_PROVIDER,
        AI_MODEL,
        AI_API_KEY,
        AI_OLLAMA_URL,
    }
}
