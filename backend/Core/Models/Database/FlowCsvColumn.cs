namespace Core.Models.Database
{
    public class FlowCsvColumn : BaseDbModel
    {
        public string Name { get; set; } = string.Empty;

        /// <summary>What was typed while recording. Never set when IsSecret.</summary>
        public string DefaultValue { get; set; } = string.Empty;

        public bool IsSecret { get; set; }
        public int OrderNumber { get; set; }

        public int FlowId { get; set; }
        public Flow Flow { get; set; } = null!;
    }
}
