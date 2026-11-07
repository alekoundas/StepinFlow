using System.Collections.ObjectModel;

namespace Core.Models.Database
{
    public class FlowTemplate : BaseDbModel
    {
        // The file name, unique in the flow ignoring case.
        public string Name { get; set; } = string.Empty;

        public byte[]? TemplateImage { get; set; } // PNG
        public byte[]? Thumbnail { get; set; } // PNG, made from TemplateImage on save.

        public int ClickOffsetX { get; set; }
        public int ClickOffsetY { get; set; }

        // The size and DPI of the area this template was captured in.
        public int AuthoredFlowAreaWidth { get; set; } 
        public int AuthoredFlowAreaHeight { get; set; }
        public int AuthoredDpi { get; set; }



        //Flow
        public int FlowId { get; set; }
        public Flow Flow { get; set; } = null!;

        public IEnumerable<FlowStepTemplate> FlowStepTemplates { get; set; } = new Collection<FlowStepTemplate>();
    }
}
