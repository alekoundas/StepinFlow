namespace Core.Models.Dtos
{
    public class FlowTemplateDto
    {
        /// <summary>0 for a picture captured in the form and not saved yet.</summary>
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public byte[]? TemplateImage { get; set; }

        public int ClickOffsetX { get; set; }
        public int ClickOffsetY { get; set; }

        public int AuthoredFlowAreaWidth { get; set; }
        public int AuthoredFlowAreaHeight { get; set; }
        public int AuthoredDpi { get; set; }

        public int FlowId { get; set; }

        /// <summary>How many steps link to it. Read only.</summary>
        public int FlowStepsCount { get; set; }
    }
}
