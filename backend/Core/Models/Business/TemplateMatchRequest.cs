using Core.Enums;

namespace Core.Models.Business
{
    public sealed class TemplateMatchRequest
    {
        public RawImage Haystack { get; set; } = new RawImage(); // Screenshot - Haystack
        public byte[] TemplateImage { get; set; } = []; // Template - Needle

        public TemplateMatchModeEnum Mode { get; set; } = TemplateMatchModeEnum.SHAPE;
        public float AccuracyThreshold { get; set; } = 0.8f;

        /// <summary>Current area size over the size the template was captured at. 1 = same.</summary>
        public float ScaleRatio { get; set; } = 1f;

        public bool AllowMultiScale { get; set; }
        public float ScaleTolerance { get; set; } = 0.15f;

        /// <summary>Stop after this many, so a bad threshold cannot return thousands.</summary>
        public int MaxMatches { get; set; } = 20;

        /// <summary>How many below the accuracy threshold candidates to report after the matches. </summary>
        public int RejectedLimit { get; set; } = 1;
    }
}
