namespace Core.Enums
{
    // Where the model's draft of a reviewed recording is.
    public enum FlowGenerationStateEnum
    {
        NONE,           // Nothing asked for, or cancelled
        GENERATING,     // Working; Stage says on what
        DONE,           // Saved, as the model's draft or the plain one; the result says which
    }
}
