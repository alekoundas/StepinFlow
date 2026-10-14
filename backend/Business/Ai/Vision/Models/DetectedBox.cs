using System.Drawing;

namespace Business.Ai.Vision.Models
{
    /// <summary>One element the detection model found, in the pixels of the image it was given.</summary>
    public sealed class DetectedBox
    {
        public RectangleF Bounds { get; init; }

        /// <summary>How sure the model is, from 0 to 1.</summary>
        public float Score { get; init; }
    }
}
