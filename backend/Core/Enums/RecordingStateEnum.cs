namespace Core.Enums
{
    public enum RecordingStateEnum
    {
        NONE,           // Nothing started, or the last recording was discarded
        RECORDING,
        PAUSED,
        PROCESSING,     // Stopped, and what it recorded is being worked through, stage by stage
        READY,          // Stopped and worked through, kept for the review
    }
}
