namespace Core.Enums
{
    /// <summary>
    /// Every persisted setting. The key alone decides the type and the default, which live in
    /// AppSettingCatalog rather than in the database.
    /// </summary>
    public enum AppSettingKeyEnum
    {
        /// <summary>Width of the screenshot the recorder takes around the cursor.</summary>
        RECORDING_CAPTURE_WIDTH,

        /// <summary>Height of the screenshot the recorder takes around the cursor.</summary>
        RECORDING_CAPTURE_HEIGHT,
    }
}
