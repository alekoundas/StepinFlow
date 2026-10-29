using Core.Enums;
using Core.Models.Business;

namespace Core.Catalogs
{
    /// <summary>
    /// The one definition of every setting, read by the service that loads them and by the
    /// Settings page that renders them, so the two cannot disagree about a default.
    /// </summary>
    public static class AppSettingCatalog
    {
        // Recording bar hotkeys. Never recorded, whatever window is in front.
        public static readonly HotkeyAppSettingDefinition RecordingHotkeyPause = new(
            AppSettingKeyEnum.RECORDING_HOTKEY_PAUSE,
            "Pause or resume",
            "Stop recording for a moment, or carry on. Nothing done while paused is recorded.",
            "VcF9");

        public static readonly HotkeyAppSettingDefinition RecordingHotkeyStop = new(
            AppSettingKeyEnum.RECORDING_HOTKEY_STOP,
            "Stop",
            "End the recording and review what was recorded.",
            "VcF10");

        // Recording gestures. The press never reaches the application, on Windows and macOS.
        public static readonly GestureAppSettingDefinition RecordingGestureCheck = new(
            AppSettingKeyEnum.RECORDING_HOTKEY_CHECK,
            RecordingGestureEnum.CHECK,
            "Check here",
            "Click something to check it is there, or that it goes. Review asks which.",
            "VcLeftControl+LEFT_BUTTON");

        public static readonly GestureAppSettingDefinition RecordingGestureSubstitute = new(
            AppSettingKeyEnum.RECORDING_HOTKEY_SUBSTITUTE,
            RecordingGestureEnum.SUBSTITUTE,
            "Do something else here",
            "Click anywhere for a step that is not a click: a command, an application opened, or an input's value typed. Review asks which.",
            "VcLeftControl+RIGHT_BUTTON");


        // Debugger hotkeys.
        public static readonly HotkeyAppSettingDefinition HotkeyContinue = new(
            AppSettingKeyEnum.HOTKEY_CONTINUE,
            "Continue",
            "Run on to the next breakpoint.",
            "VcF5");

        public static readonly HotkeyAppSettingDefinition HotkeyStepInto = new(
            AppSettingKeyEnum.HOTKEY_STEP_INTO,
            "Step into",
            "Run this step, then stop at the first step inside it.",
            "VcF11");

        public static readonly HotkeyAppSettingDefinition HotkeyStepOver = new(
            AppSettingKeyEnum.HOTKEY_STEP_OVER,
            "Step over",
            "Run this step and everything under it, then stop at the next one beside it.",
            "VcF10");

        public static readonly HotkeyAppSettingDefinition HotkeyPause = new(
            AppSettingKeyEnum.HOTKEY_PAUSE,
            "Pause",
            "Stop after the step that is running now. The only one pressed while a flow is typing.",
            "VcF9");

        public static readonly HotkeyAppSettingDefinition HotkeyStop = new(
            AppSettingKeyEnum.HOTKEY_STOP,
            "Stop",
            "End the run.",
            "VcF8");

        public static readonly IntAppSettingDefinition ExecutionScreenshotLimit = new IntAppSettingDefinition(
            AppSettingKeyEnum.EXECUTION_SCREENSHOT_LIMIT,
            "Screenshots kept per execution",
            "How many screenshots one execution may leave on disk, whether the steps worked or not. Raise it if a long flow stops keeping them before the part you care about.",
            defaultValue: 20,
            minimum: 0,
            maximum: 500);


        // AI. Nothing is on until Ollama answers with the local model chosen here: every feature
        // checks first and stays disabled rather than failing at the moment somebody clicks it.
        public static readonly TextAppSettingDefinition AiOllamaModel = new TextAppSettingDefinition(
            AppSettingKeyEnum.AI_OLLAMA_MODEL,
            "Model",
            "Which model Ollama runs, out of the ones downloaded onto this machine. Every AI feature needs one.");

        // A cloud model is added on top of the local one, never instead of it.
        public static readonly ChoiceAppSettingDefinition AiCloudProvider = new ChoiceAppSettingDefinition(
            AppSettingKeyEnum.AI_CLOUD_PROVIDER,
            "Cloud model",
            "Optional. Answers in the local model's place, faster and better, but needs a key and sends data away.",
            defaultValue: nameof(AiCloudProviderEnum.NONE),
            options: [nameof(AiCloudProviderEnum.NONE), nameof(AiCloudProviderEnum.OPENAI)]);

        public static readonly TextAppSettingDefinition AiCloudModel = new TextAppSettingDefinition(
            AppSettingKeyEnum.AI_CLOUD_MODEL,
            "Model",
            "Which of the cloud provider's models to ask.");

        public static readonly TextAppSettingDefinition AiApiKey = new TextAppSettingDefinition(
            AppSettingKeyEnum.AI_API_KEY,
            "API key",
            "The cloud provider's key. Kept on this machine and sent only to that provider.",
            isSecret: true);

        public static readonly BoolAppSettingDefinition AiSendScreenContent = new BoolAppSettingDefinition(
            AppSettingKeyEnum.AI_SEND_SCREEN_CONTENT,
            "Let a cloud model see your screen",
            "Off by default. The screenshots a run kept, text read by OCR, and whatever a flow was recorded typing are all whatever was on your screen - an account number, a message, a password field. A local model never leaves this machine, so this only ever applies to a cloud provider.",
            defaultValue: false);

        // Ollama serves 4096 whatever the model can hold, and that is not enough for the prompt,
        // the tools and a screenshot together. Bigger costs memory: the key value cache grows with
        // it, so a machine that is tight on ram wants this lower.
        public static readonly IntAppSettingDefinition AiOllamaContextLength = new IntAppSettingDefinition(
            AppSettingKeyEnum.AI_OLLAMA_CONTEXT_LENGTH,
            "Context window to ask Ollama for",
            "How much the model may hold at once. 4096 is Ollama's default and too small once screenshots are involved. Higher uses more memory.",
            defaultValue: 32768,
            minimum: 4096,
            maximum: 131072);

        public static readonly TextAppSettingDefinition AiOllamaUrl = new TextAppSettingDefinition(
            AppSettingKeyEnum.AI_OLLAMA_URL,
            "Ollama address",
            "Where Ollama is listening. The default is right unless you moved it.",
            defaultValue: "http://localhost:11434");

        // Repository. Empty takes the user's git config, which most people who use git already have.
        public static readonly TextAppSettingDefinition GitAuthorName = new TextAppSettingDefinition(
            AppSettingKeyEnum.GIT_AUTHOR_NAME,
            "Name",
            "Who every commit is by.");

        public static readonly TextAppSettingDefinition GitAuthorEmail = new TextAppSettingDefinition(
            AppSettingKeyEnum.GIT_AUTHOR_EMAIL,
            "Email",
            "Written into every commit beside the name.");

        // A plain setting for now (TODO.md, Git): the system's credential store is per system.
        public static readonly TextAppSettingDefinition GitRemoteToken = new TextAppSettingDefinition(
            AppSettingKeyEnum.GIT_REMOTE_TOKEN,
            "Token",
            "Made on the provider's site, for this repository only, allowed to read and write its contents.",
            isSecret: true);

        public static IReadOnlyList<AppSettingDefinition> All { get; } =
        [
            RecordingHotkeyPause,
            RecordingHotkeyStop,
            RecordingGestureCheck,
            RecordingGestureSubstitute,

            HotkeyContinue,
            HotkeyStepInto,
            HotkeyStepOver,
            HotkeyPause,
            HotkeyStop,

            ExecutionScreenshotLimit,

            AiOllamaModel,
            AiOllamaUrl,
            AiOllamaContextLength,
            AiCloudProvider,
            AiCloudModel,
            AiApiKey,
            AiSendScreenContent,

            GitAuthorName,
            GitAuthorEmail,
            GitRemoteToken,
        ];

        /// <summary>In the order the Settings page lists them.</summary>
        public static IReadOnlyList<HotkeyAppSettingDefinition> Hotkeys { get; } =
        [
            RecordingHotkeyPause,
            RecordingHotkeyStop,

            HotkeyContinue,
            HotkeyStepInto,
            HotkeyStepOver,
            HotkeyPause,
            HotkeyStop,
        ];

        public static AppSettingDefinition? Find(AppSettingKeyEnum key)
        {
            return All.FirstOrDefault(x => x.Key == key);
        }
    }
}
