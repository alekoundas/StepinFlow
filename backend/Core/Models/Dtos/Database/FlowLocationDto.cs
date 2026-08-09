using Core.Enums;

namespace Core.Models.Dtos
{
    public class FlowLocationDto
    {
        /// <summary>0 for a new row. Negative for a new row that references another new row.</summary>
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public int? FlowSearchAreaId { get; set; }
        public AreaSizingModeEnum OffsetMode { get; set; }

        public int LocationX { get; set; }
        public int LocationY { get; set; }

        public float RatioX { get; set; }
        public float RatioY { get; set; }

        public int FlowId { get; set; }

        // Read only, projected by the query.
        public int FlowStepsCount { get; set; }
        public string FlowSearchAreaName { get; set; } = string.Empty;
    }
}
