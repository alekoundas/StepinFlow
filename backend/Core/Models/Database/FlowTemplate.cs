using System.Collections.ObjectModel;

namespace Core.Models.Database
{
    public class FlowTemplate : BaseDbModel
    {
        // The file name, unique in the flow ignoring case.
        public string Name { get; set; } = string.Empty;

        public byte[]? TemplateImage { get; set; } // PNG
        public byte[]? Thumbnail { get; set; } // PNG, made from TemplateImage whenever it is saved

        public int ClickOffsetX { get; set; }
        public int ClickOffsetY { get; set; }

        // The size of the area this template was captured in, and the DPI it was captured at - the two things a different screen can change about it.
        public int AuthoredFlowAreaWidth { get; set; }
        public int AuthoredFlowAreaHeight { get; set; }
        public int AuthoredDpi { get; set; }


        public int FlowId { get; set; }
        public Flow Flow { get; set; } = null!;

        public IEnumerable<FlowStepTemplate> FlowStepTemplates { get; set; } = new Collection<FlowStepTemplate>();
    }
}
