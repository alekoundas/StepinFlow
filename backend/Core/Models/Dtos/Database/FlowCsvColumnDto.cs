namespace Core.Models.Dtos
{
    public class FlowCsvColumnDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;

        /// <summary>What was typed while recording. Never set when IsSecret.</summary>
        public string DefaultValue { get; set; } = string.Empty;

        public bool IsSecret { get; set; }
        public int OrderNumber { get; set; }

        public int FlowId { get; set; }
    }
}
