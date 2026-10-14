namespace Core.Enums
{
    // How the element under a click was found.
    public enum ElementFoundByEnum
    {
        MODEL,          // The detection model's rectangle
        FALLBACK,       // A box around the click, when the model had nothing to say
    }
}
