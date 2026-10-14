namespace Core.Enums
{
    // What a stopped recording is being worked through, in order.
    public enum RecordingStageEnum
    {
        NONE,               // Not processing
        FINDING_ELEMENTS,   // What each click hit, by the detection model and OCR
    }
}
