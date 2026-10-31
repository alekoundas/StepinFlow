namespace Core.Enums
{
    // What a generation is working on, in order. Reading an answer back takes no time to speak of,
    // so it is part of asking: a refused answer is asked for again.
    public enum FlowGenerationStageEnum
    {
        NONE,               // Not generating
        LOOKING_AT_SCREENS, // The local model naming wordless clicks and describing new pages
        WRITING_DRAFT,      // The plain draft, as a script
        WAITING_TO_SEND,    // A cloud model's request, with text read off the screen in it, shown until the tester sends it
        ASKING_MODEL,       // The model shaping it, one attempt after another
        SAVING,             // Imported into the flow, and validated
    }
}
