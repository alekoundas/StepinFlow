namespace Core.Enums
{
    public enum RecordingStateEnum
    {
        NONE,           // Nothing started, or the last recording was discarded
        RECORDING,
        PAUSED,
        STOPPED,        // Ended, and what it recorded is kept for the review
    }
}
