namespace Core.Enums
{
    // Which window a press landed on, read at the press itself.
    public enum RecordedWindowEnum
    {
        MAIN_AREA,          // The window the flow works in
        SAME_APPLICATION,   // Another window of its application: a Save As dialog, a menu
        OTHER,              // Anything else, left out until the tester keeps it
    }
}
