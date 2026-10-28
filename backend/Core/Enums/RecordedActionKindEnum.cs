namespace Core.Enums
{
    /// <summary>What a coalesced recording action was, which decides what Review offers for it.</summary>
    public enum RecordedActionKindEnum
    {
        CLICK,
        DRAG,
        SCROLL,
        TYPING,
        KEY_COMBINATION,
        KEY_HOLD,       // A modifier going down and staying down over a click or a scroll
        KEY_RELEASE,    // That modifier coming back up
        PAUSE,
        CHECK,          // A press marking something to check at that spot, the gesture's
        SUBSTITUTE,     // A press marking a step that is not a click, the gesture's
    }
}
