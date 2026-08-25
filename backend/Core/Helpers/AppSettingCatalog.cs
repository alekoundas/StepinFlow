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

        public static IReadOnlyList<AppSettingDefinition> All { get; } =
        [
            RecordingCaptureWidth,
            RecordingCaptureHeight,

            HotkeyContinue,
            HotkeyStepInto,
            HotkeyStepOver,
            HotkeyPause,
            HotkeyStop,
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

        public static AppSettingDefinition? Find(AppSettingKeyEnum key) =>
            All.FirstOrDefault(x => x.Key == key);
    }
}
