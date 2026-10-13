using Core.Models.Business;

namespace Business.Recording.Models.Session
{
    /// <summary>
    /// One screenshot of the main area, stamped when the capture finished: one stamped before a
    /// press shows nothing the press changed.
    /// </summary>
    public sealed class RecordingScreenshot
    {
        public RecordingScreenshot(DateTime capturedOn, RecordingTarget target, RawImage image)
        {
            CapturedOn = capturedOn;
            Target = target;
            Image = image;
        }

        public DateTime CapturedOn { get; }
        public RecordingTarget Target { get; }
        public RawImage Image { get; }
    }
}
