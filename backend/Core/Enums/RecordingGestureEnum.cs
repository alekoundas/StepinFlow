namespace Core.Enums
{
    /// <summary>
    /// A press while recording that marks something rather than clicks: held back from the
    /// application, and recorded as what it marks.
    /// </summary>
    public enum RecordingGestureEnum
    {
        CHECK,          // Something to check at that spot
        SUBSTITUTE,     // Something done there instead of a click
    }
}
