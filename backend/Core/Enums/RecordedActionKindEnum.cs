namespace Core.Enums
{
    /// <summary>What a coalesced recording action was, which decides what the wizard can offer for it.</summary>
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
    }
}
