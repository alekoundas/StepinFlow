using Core.Enums;
using Core.Models.Business;

namespace Core.Helpers
{
    /// <summary>
    /// The one definition of every setting, read by the service that loads them and by the
    /// Settings page that renders them, so the two cannot disagree about a default.
    /// </summary>
    public static class AppSettingCatalog
    {
        public static readonly IntAppSettingDefinition RecordingCaptureWidth = new(
            AppSettingKeyEnum.RECORDING_CAPTURE_WIDTH,
            "Capture width",
            "How wide a screenshot the recorder takes around the pointer.",
            defaultValue: 400,
            minimum: 50,
            maximum: 4000);

        public static readonly IntAppSettingDefinition RecordingCaptureHeight = new(
            AppSettingKeyEnum.RECORDING_CAPTURE_HEIGHT,
            "Capture height",
            "How tall a screenshot the recorder takes around the pointer.",
            defaultValue: 400,
            minimum: 50,
            maximum: 4000);


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

        // Nothing is written while a flow is going well. A failure writes out the screenshots
        // leading up to it, which is where the cause is - the one taken at the failure only shows
        // a screen the thing was not on.
        public static readonly IntAppSettingDefinition ExecutionScreenshotRingSize = new IntAppSettingDefinition(
            AppSettingKeyEnum.EXECUTION_SCREENSHOT_RING_SIZE,
            "Screenshots kept before a failure",
            "How much run-up is saved with a failed step. Nothing is written while a flow succeeds.",
            defaultValue: 5,
            minimum: 5,
            maximum: 50);

        // Written until the count is reached and then no more, rather than kept rolling: a search
        // inside a loop would otherwise write a screenshot on every pass of an unattended run.
        public static readonly IntAppSettingDefinition ExecutionScreenshotLimit = new IntAppSettingDefinition(
            AppSettingKeyEnum.EXECUTION_SCREENSHOT_LIMIT,
            "Screenshots kept per run",
            "How many screenshots a run leaves behind, so you can see what a step was looking at. Zero keeps none.",
            defaultValue: 20,
            minimum: 0,
            maximum: 500);


        // AI. Nothing is on until a provider is chosen: every feature checks first and stays
        // disabled rather than failing at the moment somebody clicks it.
        public static readonly ChoiceAppSettingDefinition AiProvider = new ChoiceAppSettingDefinition(
            AppSettingKeyEnum.AI_PROVIDER,
            "AI provider",
            "Where the model runs. Ollama keeps everything on this machine; OpenAI is faster and better but needs a key and sends data away.",
            defaultValue: nameof(AiProviderEnum.NONE),
            options: [nameof(AiProviderEnum.NONE), nameof(AiProviderEnum.OLLAMA), nameof(AiProviderEnum.OPENAI)]);

        public static readonly TextAppSettingDefinition AiModel = new TextAppSettingDefinition(
            AppSettingKeyEnum.AI_MODEL,
            "Model",
            "Which model to ask. For Ollama this is whatever you have pulled, for example qwen2.5.");

        public static readonly TextAppSettingDefinition AiApiKey = new TextAppSettingDefinition(
            AppSettingKeyEnum.AI_API_KEY,
            "API key",
            "Only needed for a provider that charges. Ollama ignores it.",
            isSecret: true);

        public static readonly TextAppSettingDefinition AiOllamaUrl = new TextAppSettingDefinition(
            AppSettingKeyEnum.AI_OLLAMA_URL,
            "Ollama address",
            "Where Ollama is listening. The default is right unless you moved it.",
            defaultValue: "http://localhost:11434");

        public static IReadOnlyList<AppSettingDefinition> All { get; } =
        [
            RecordingCaptureWidth,
            RecordingCaptureHeight,

            HotkeyContinue,
            HotkeyStepInto,
            HotkeyStepOver,
            HotkeyPause,
            HotkeyStop,

            ExecutionScreenshotRingSize,
            ExecutionScreenshotLimit,

            AiProvider,
            AiModel,
            AiApiKey,
            AiOllamaUrl,
        ];

        /// <summary>In the order the Settings page lists them.</summary>
        public static IReadOnlyList<HotkeyAppSettingDefinition> Hotkeys { get; } =
        [
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
