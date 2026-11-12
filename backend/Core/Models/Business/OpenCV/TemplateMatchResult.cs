using Core.Enums;

namespace Core.Models.Business.OpenCV
{
    /// <summary>One hit, in coordinates relative to the searched area.</summary>
    public sealed class TemplateMatchResult
    {
        public int X { get; set; }
        public int Y { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }

        /// <summary>The shape score, which the accuracy is read against in both modes.</summary>
        public float Score { get; set; }

        /// <summary>What the template was resized by to match. 1 = not resized.</summary>
        public float Scale { get; set; }

        /// <summary>Shape and colour: how far apart the average colours are, lightness left out.</summary>
        public float? Colour { get; set; }

        /// <summary>The most Colour may be for this template.</summary>
        public float? ColourLimit { get; set; }

        /// <summary>Shape and colour: how widely the lightness varies here, over how widely it varies in the template.</summary>
        public float? Contrast { get; set; }

        /// <summary>Set when the shape passed and the colour or the contrast did not.</summary>
        public MatchRefusalEnum? RefusedBy { get; set; }
    }
}
