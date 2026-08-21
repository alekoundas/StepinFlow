using Core.Enums;

namespace Core.Models.Dtos
{
    public class SetAppSettingDto
    {
        public AppSettingKeyEnum Key { get; set; }
        public string Value { get; set; } = string.Empty;
    }
}
