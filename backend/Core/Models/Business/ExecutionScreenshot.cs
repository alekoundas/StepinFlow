namespace Core.Models.Business
{
    /// <summary>
    /// One screenshot in the ring, and who took it. The name is copied because the ring outlives
    /// the step that filled it - a failure dumps whatever is in there, most of it somebody else's.
    /// </summary>
    public class ExecutionScreenshot
    {
        public ExecutionScreenshot(byte[] image, string stepName, DateTime capturedOn)
        {
            Image = image;
            StepName = stepName;
            CapturedOn = capturedOn;
        }

        public byte[] Image { get; }
        public string StepName { get; }
        public DateTime CapturedOn { get; }
    }
}
