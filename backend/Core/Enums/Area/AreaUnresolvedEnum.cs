namespace Core.Enums
{
    /// <summary>
    /// Why an area was not found, which decides whether trying again can help. A wait tries again
    /// on NOT_ON_SCREEN and NO_SIZE, and gives up at once on NOT_SET_UP.
    /// </summary>
    public enum AreaUnresolvedEnum
    {
        // Deleted, or saved so that it can never be found.
        NOT_SET_UP,

        // Its window, tab or monitor is not there now.
        NOT_ON_SCREEN,

        // There, but with nothing to measure now: minimised, or outside the area it sits in.
        NO_SIZE,
    }
}
