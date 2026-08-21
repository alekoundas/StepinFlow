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

        public static IReadOnlyList<AppSettingDefinition> All { get; } =
        [
            RecordingCaptureWidth,
            RecordingCaptureHeight,
        ];

        public static AppSettingDefinition? Find(AppSettingKeyEnum key) =>
            All.FirstOrDefault(x => x.Key == key);
    }
}
