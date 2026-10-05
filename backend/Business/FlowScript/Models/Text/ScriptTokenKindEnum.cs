namespace Business.FlowScript.Models.Text
{
    internal enum ScriptTokenKindEnum
    {
        // A word or symbol from the catalog, "Wait Until No Image" and "<[" alike.
        KEYWORD,

        // Text as written: between <[ and ]>, or the rest of the line after a keyword that takes it.
        QUOTE,

        // Unquoted and starting with a digit or a minus: 0.85, -10, 800ms, 120dpi, 1920x1080.
        NUMBER,

        // Anything else unquoted. Nothing in the grammar takes one, so the parser reports it.
        UNKNOWN,

        // Always last, just past the line's text.
        END_OF_LINE,
    }
}
