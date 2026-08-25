using Core.Enums;

namespace Core.Models.Dtos
{
    public class AppSettingDto
    {
        public AppSettingKeyEnum Key { get; set; }
        public AppSettingKindEnum Kind { get; set; }
        public string Label { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;

        public string Value { get; set; } = string.Empty;
        public int? Minimum { get; set; }
        public int? Maximum { get; set; }
    }
}
