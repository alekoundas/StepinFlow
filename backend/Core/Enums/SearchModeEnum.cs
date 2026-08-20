namespace Core.Enums
{
    /// <summary>
    /// What a screen search does, for both IMAGE_SEARCH and READ_TEXT.
    ///
    /// One axis, not two: acting on every match only ever made sense while looking once, so it is
    /// a mode rather than a flag that would be dead in three of four of them.
    ///
    /// The waiting modes poll the area every PollIntervalMilliseconds until TimeoutMilliseconds
    /// runs out, or forever when it is 0.
    /// </summary>
    public enum SearchModeEnum
    {
        /// <summary>Look once, act on the strongest match.</summary>
        FIND_BEST,

        /// <summary>Look once, act on every match, capped by MaxMatches. IMAGE_SEARCH only.</summary>
        FIND_ALL,

        WAIT_UNTIL_FOUND,

        /// <summary>
        /// Not "until gone": nothing verifies the thing was ever there. It waits for the search to
        /// stop matching, which succeeds immediately if it never matched at all.
        /// </summary>
        WAIT_UNTIL_NOT_FOUND,
    }
}
