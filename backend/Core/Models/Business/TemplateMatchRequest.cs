using Core.Enums;

namespace Core.Models.Business
{
    public sealed class TemplateMatchRequest
    {
        public RawImage Haystack { get; set; } = new RawImage();

        /// <summary>Encoded template, as stored on FlowStepImage.</summary>
        public byte[] TemplateImage { get; set; } = [];

        public TemplateMatchModeEnum Mode { get; set; } = TemplateMatchModeEnum.CCoeffNormed;
        public float Threshold { get; set; } = 0.8f;

        /// <summary>Current area size over the size the template was captured at. 1 = same.</summary>
        public float ScaleRatio { get; set; } = 1f;

        public bool AllowMultiScale { get; set; }
        public float ScaleTolerance { get; set; } = 0.15f;

        /// <summary>Stop after this many, so a bad threshold cannot return thousands.</summary>
        public int MaxMatches { get; set; } = 20;

        /// <summary>
        /// How many below-threshold candidates to report after the matches. One is enough to show
        /// where the cut fell and what it cost.
        /// </summary>
        public int RejectedLimit { get; set; } = 1;
    }
}
