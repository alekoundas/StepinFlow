namespace Core.Enums
{
    public enum AppSettingKeyEnum
    {
        // Recording bar hotkeys.
        RECORDING_HOTKEY_PAUSE,
        RECORDING_HOTKEY_STOP,
        RECORDING_HOTKEY_STAGE,
        RECORDING_HOTKEY_CHECK,
        RECORDING_HOTKEY_SUBSTITUTE,


        // Debugger hotkeys.
        HOTKEY_CONTINUE,
        HOTKEY_STEP_INTO,
        HOTKEY_STEP_OVER,
        HOTKEY_PAUSE,
        HOTKEY_STOP,

        EXECUTION_SCREENSHOT_LIMIT,


        /// <summary>The local model, which every AI feature needs, and where Ollama listens.</summary>
        AI_OLLAMA_MODEL,
        AI_OLLAMA_URL,

        /// <summary>How big a context window to ask Ollama for, since it serves 4096 by default.</summary>
        AI_OLLAMA_CONTEXT_LENGTH,

        /// <summary>A cloud model on top of the local one, and what it needs to reach it.</summary>
        AI_CLOUD_PROVIDER,
        AI_CLOUD_MODEL,
        AI_API_KEY,

        /// <summary>Whether a cloud model may be shown what was on screen - read text and screenshots.</summary>
        AI_SEND_SCREEN_CONTENT,

        /// <summary>Who a commit is by. Empty takes the user's git config.</summary>
        GIT_AUTHOR_NAME,
        GIT_AUTHOR_EMAIL,

        /// <summary>The token a push and a fetch sign in to the remote with. Never sent to the page.</summary>
        GIT_REMOTE_TOKEN,
    }
}
