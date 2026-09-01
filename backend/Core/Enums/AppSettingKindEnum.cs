namespace Core.Enums
{
    /// <summary>What kind of control a setting needs, so the Settings page does not branch on the key.</summary>
    public enum AppSettingKindEnum
    {
        INT,
        HOTKEY,
        TEXT,

        /// <summary>Text the page must never show back in full - an api key.</summary>
        SECRET,

        /// <summary>One of a fixed list.</summary>
        CHOICE,
        BOOL,
    }
}
