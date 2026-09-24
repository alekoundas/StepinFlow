using System.Drawing;

using Core.Enums;
using Core.Models.Business;
using Core.Models.Database;
using Core.Models.Dtos;

namespace Business.Searching
{
    /// <summary>
    /// One template to look for, as the matcher needs it.
    ///
    /// The stored row and the unsaved form both become this. That is the point: the editor's test
    /// button and the execution engine ask the same question, so the preview cannot promise
    /// something the run will not do.
    /// </summary>
    public sealed record SearchTemplate
    {
        public byte[] Image { get; init; } = [];
        public TemplateMatchModeEnum? Mode { get; init; }
        public float? Accuracy { get; init; }
        public int AuthoredFrameWidth { get; init; }
        public bool AllowMultiScale { get; init; }
        public float ScaleTolerance { get; init; }
        public int ClickOffsetX { get; init; }
        public int ClickOffsetY { get; init; }

        public static SearchTemplate From(FlowStepTemplate template)
        {
            return new SearchTemplate
            {
                Image = template.TemplateImage ?? [],
                Mode = template.TemplateMatchMode,
                Accuracy = template.Accuracy,
                AuthoredFrameWidth = template.AuthoredFrameWidth,
                AllowMultiScale = template.AllowMultiScale,
                ScaleTolerance = template.ScaleTolerance,
                ClickOffsetX = template.ClickOffsetX,
                ClickOffsetY = template.ClickOffsetY,
            };
        }

        public static SearchTemplate From(FlowStepTemplateDto template)
        {
            return new SearchTemplate
            {
                Image = template.TemplateImage ?? [],
                Mode = template.TemplateMatchMode,
                Accuracy = template.Accuracy,
                AuthoredFrameWidth = template.AuthoredFrameWidth,
                AllowMultiScale = template.AllowMultiScale,
                ScaleTolerance = template.ScaleTolerance,
                ClickOffsetX = template.ClickOffsetX,
                ClickOffsetY = template.ClickOffsetY,
            };
        }

        /// <summary>
        /// Where a click lands for this match, relative to the search area. The offset is scaled
        /// with the template, so a match found at 120% moves its click point by 120% too.
        /// </summary>
        public Point ClickPoint(TemplateMatchResult match)
        {
            return new Point(
                match.X + (int)MathF.Round(ClickOffsetX * match.Scale),
                match.Y + (int)MathF.Round(ClickOffsetY * match.Scale));
        }
    }
}
