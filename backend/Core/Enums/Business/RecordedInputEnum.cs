namespace Core.Enums
{
    public enum RecordedInputTypeEnum
    {
        // Cursor
        BUTTON_UP,
        BUTTON_DOWN,
        CURSOR_DRAG,
        CURSOR_MOVE,
        CURSOR_SCROLL,

        // Keyboard
        KEY_UP,
        KEY_DOWN,
        KEY_TYPED,  // What a key-down typed, as the layout made it, raised right after it

        // Session: the tester resumed after a pause, so the gap before this is not a wait
        RESUMED,

        // Session: the tester marked where a stage of the test begins
        STAGE,
    }
}
