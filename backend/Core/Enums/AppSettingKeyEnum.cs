namespace Core.Enums
{
    public enum AppSettingKeyEnum
    {
        // Recording bar hotkeys.
        RECORDING_HOTKEY_PAUSE,
        RECORDING_HOTKEY_STOP,


        // Debugger hotkeys.
        HOTKEY_CONTINUE,
        HOTKEY_STEP_INTO,
        HOTKEY_STEP_OVER,
        HOTKEY_PAUSE,
        HOTKEY_STOP,

        EXECUTION_SCREENSHOT_LIMIT,


        /// <summary>Who runs the model, and what it needs to reach it.</summary>
        AI_PROVIDER,
        AI_MODEL,
        AI_API_KEY,
        AI_OLLAMA_URL,

        /// <summary>Whether a cloud provider may be shown what was on screen - read text and screenshots.</summary>
        AI_SEND_SCREEN_CONTENT,

        /// <summary>How big a context window to ask Ollama for, since it serves 4096 by default.</summary>
        AI_OLLAMA_CONTEXT_LENGTH,

        /// <summary>Who a commit is by. Empty takes the user's git config.</summary>
        GIT_AUTHOR_NAME,
        GIT_AUTHOR_EMAIL,
    }
}
