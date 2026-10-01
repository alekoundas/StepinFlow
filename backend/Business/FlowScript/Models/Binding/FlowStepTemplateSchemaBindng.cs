
using System.Drawing;

namespace Business.FlowScript.Models.Binding
{
    public sealed class FlowStepTemplateSchemaBindng
    {
        public string FileName { get; init; } = string.Empty;
        public Point? ClickOffset { get; set; }
        public int AuthoredFlowAreaWidth { get; set; }
        public int AuthoredFlowAreaHeight { get; set; }
        public int AuthoredDpi { get; set; }
        public int Line { get; init; }
    }

}
