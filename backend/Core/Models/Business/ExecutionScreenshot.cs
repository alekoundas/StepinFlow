namespace Core.Models.Business
{
    /// <summary>
    /// One screenshot an execution took, and who took it. Held in memory while it is one of the
    /// execution's last few, and written to disk as the execution ends.
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
