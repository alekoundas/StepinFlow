namespace Core.Enums
{
    // What the tester said a recorded action becomes, in Review.
    public enum ReviewChoiceEnum
    {
        KEEP,               // As it was: a key pressed, a scroll, a click outside the main area kept
        WAIT_AND_CLICK,     // Wait for the element, then click wherever it was found
        CLICK_AT_SPOT,      // Click the place it was clicked, without looking for anything
        INPUT,              // Typed text that becomes an input, with what was typed as its default
        TYPE_AS_WRITTEN,    // Typed text written into the flow as it is
        LEAVE_OUT,
    }
}
