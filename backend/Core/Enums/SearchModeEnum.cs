namespace Core.Enums
{
    /// <summary>
    /// What a screen search does, for both SEARCH_IMAGE and SEARCH_TEXT.
    ///
    /// One axis, not two: acting on every match only ever made sense while looking once, so it is
    /// a mode rather than a flag that would be dead in three of four of them.
    ///
    /// The waiting modes poll the area every PollIntervalMilliseconds until TimeoutMilliseconds
    /// runs out, or forever when it is 0.
    /// </summary>
    public enum SearchModeEnum
    {
        FIND_BEST,

        FIND_ALL,

        WAIT_UNTIL_FOUND,

        WAIT_UNTIL_NOT_FOUND,
    }
}
