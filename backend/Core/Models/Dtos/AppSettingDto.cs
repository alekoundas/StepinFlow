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

        /// <summary>CHOICE only: what the dropdown offers.</summary>
        public IReadOnlyList<string> Options { get; set; } = [];

        /// <summary>
        /// SECRET only. The value never leaves the backend for these - the page shows whether one
        /// is set, and writes a new one, but is never told the old.
        /// </summary>
        public bool IsSet { get; set; }
    }
}
