using System.Drawing;
using Core.Models.Business.OpenCV;
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
        public byte[] Image { get; set; } = [];
        public float Accuracy { get; set; }
        public bool IsRequired { get; set; }
        public int AuthoredFlowAreaWidth { get; set; }
        public int AuthoredFlowAreaHeight { get; set; }
        public int AuthoredDpi { get; set; }
        public int ClickOffsetX { get; set; }
        public int ClickOffsetY { get; set; }

        public static SearchTemplate From(FlowStepTemplate template)
        {
            FlowTemplate picture = template.FlowTemplate;

            return new SearchTemplate
            {
                Image = picture.TemplateImage ?? [],
                Accuracy = template.Accuracy,
                IsRequired = template.IsRequired,
                AuthoredFlowAreaWidth = picture.AuthoredFlowAreaWidth,
                AuthoredFlowAreaHeight = picture.AuthoredFlowAreaHeight,
                AuthoredDpi = picture.AuthoredDpi,
                ClickOffsetX = picture.ClickOffsetX,
                ClickOffsetY = picture.ClickOffsetY,
            };
        }

        public static SearchTemplate From(FlowStepTemplateDto template)
        {
            FlowTemplateDto picture = template.FlowTemplate ?? new FlowTemplateDto();

            return new SearchTemplate
            {
                Image = picture.TemplateImage ?? [],
                Accuracy = template.Accuracy,
                IsRequired = template.IsRequired,
                AuthoredFlowAreaWidth = picture.AuthoredFlowAreaWidth,
                AuthoredFlowAreaHeight = picture.AuthoredFlowAreaHeight,
                AuthoredDpi = picture.AuthoredDpi,
                ClickOffsetX = picture.ClickOffsetX,
                ClickOffsetY = picture.ClickOffsetY,
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
