using Core.Enums;

namespace Core.Models.Database
{
    /// <summary>
    /// One changed setting. Rows only exist once a setting has been moved off its default, so an
    /// empty table is a fully default install.
    ///
    /// The value is text for every setting.
    /// </summary>
    public class AppSetting
    {
        public AppSettingKeyEnum Key { get; set; }
        public string Value { get; set; } = string.Empty;
    }
}
