namespace Core.Enums
{
    /// <summary>
    /// When an image search stops looking. The two waiting modes poll the search area every
    /// PollIntervalMilliseconds until TimeoutMilliseconds runs out, or forever when it is 0.
    /// </summary>
    public enum ImageSearchModeEnum
    {
        FIND_ONCE,
        WAIT_UNTIL_FOUND,
        WAIT_UNTIL_GONE,
    }
}
