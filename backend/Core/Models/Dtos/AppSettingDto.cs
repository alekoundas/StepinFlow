using Core.Enums;

namespace Core.Models.Dtos
{
    /// <summary>A setting as the Settings page needs it: what it is, plus what it currently says.</summary>
    public class AppSettingDto
    {
        public AppSettingKeyEnum Key { get; set; }
        public string Label { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;

        public string Value { get; set; } = string.Empty;
        public int? Minimum { get; set; }
        public int? Maximum { get; set; }
    }
}
