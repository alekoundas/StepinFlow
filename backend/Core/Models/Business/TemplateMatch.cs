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

        /// <summary>Current frame size over the size the template was captured at. 1 = same.</summary>
        public float ScaleRatio { get; set; } = 1f;

        public bool AllowMultiScale { get; set; }
        public float ScaleTolerance { get; set; } = 0.15f;

        /// <summary>Stop after this many, so a bad threshold cannot return thousands.</summary>
        public int MaxMatches { get; set; } = 20;
    }

    public sealed record TemplateMatch(
        int X,
        int Y,
        int Width,
        int Height,
        float Score,
        float Scale)
    {
        public int CenterX => X + Width / 2;
        public int CenterY => Y + Height / 2;
    }
}
