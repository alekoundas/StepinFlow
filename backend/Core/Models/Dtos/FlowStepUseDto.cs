using Core.Enums;

namespace Core.Models.Dtos
{
    /// <summary>
    /// A step and the parts of its flow it uses: the area it looks in, the locations it moves to,
    /// the {{names}} it fills in and the templates it searches for.
    /// </summary>
    public class FlowStepUseDto
    {
        public int FlowStepId { get; set; }
        public string Name { get; set; } = string.Empty;
        public FlowStepTypeEnum FlowStepType { get; set; }

        public int? FlowAreaId { get; set; }
        public int? FlowPointId { get; set; }
        public int? FlowPointEndId { get; set; }

        /// <summary>Every {{name}} in its text: an input's, another step's or a viewport's.</summary>
        public List<string> VariableNames { get; set; } = new List<string>();
        public List<string> TemplateNames { get; set; } = new List<string>();
    }
}
