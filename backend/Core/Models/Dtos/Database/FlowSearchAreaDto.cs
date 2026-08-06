using Core.Enums;

namespace Core.Models.Dtos
{
    public class FlowSearchAreaDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public FlowSearchAreaTypeEnum Type { get; set; }


        public string AppWindowName { get; set; } = string.Empty;
        public string MonitorUniqueId { get; set; } = string.Empty;


        // Custom search area
        public int LocationX { get; set; }
        public int LocationY { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }

        // Flow
        public int FlowId { get; set; }

        // How many FlowSteps use this search area. Read only, projected by the query.
        public int FlowStepsCount { get; set; }
    }
}
