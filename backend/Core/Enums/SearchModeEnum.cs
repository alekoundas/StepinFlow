namespace Core.Enums
{
    public enum SearchModeEnum
    {
        FIND_BEST,

        FIND_ALL,

        WAIT_UNTIL_FOUND,

        // Images only: found in the same place twice, so a click does not land where an animation was.
        WAIT_UNTIL_STILL,

        WAIT_UNTIL_NOT_FOUND,
    }
}
