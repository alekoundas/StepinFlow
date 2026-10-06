namespace Business.FlowScript.Models.Text
{
    internal enum ScriptTokenKindEnum
    {
        KEYWORD,     // A word or symbol from the catalog.
        QUOTE,       // Text as written between <[ and ]> and CodeComment(#) and StageMarker(##) and FlowFields.
        NUMBER,      // Unquoted and starting with a digit or a minus: 0.85, -10, 800ms, 120dpi, 1920x1080.
        UNKNOWN,     // Nothing in the grammar knows about, parser will throw error.
        END_OF_LINE, // Always last, past the line text.
    }
}
