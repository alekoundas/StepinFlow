namespace Core.Models.Dtos
{
    public class FlowStepTemplateDto
    {
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;
        public int OrderNumber { get; set; }

        public byte[]? TemplateImage { get; set; }

        public bool IsRequired { get; set; }

        public float Accuracy { get; set; } = 0.8f;

        public int ClickOffsetX { get; set; }
        public int ClickOffsetY { get; set; }

        public int AuthoredFlowAreaWidth { get; set; }
        public int AuthoredFlowAreaHeight { get; set; }
        public int AuthoredDpi { get; set; }

        public int FlowStepId { get; set; }
    }
}
