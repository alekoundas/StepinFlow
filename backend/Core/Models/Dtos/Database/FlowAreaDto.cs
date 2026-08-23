using Core.Enums;

namespace Core.Models.Dtos
{
    public class FlowAreaDto
    {
        /// <summary>0 for a new row. Negative for a new row another new row points at.</summary>
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;
        public FlowAreaTypeEnum Type { get; set; }


        // CUSTOM
        public int? ParentFlowAreaId { get; set; }
        public AreaSizingModeEnum SizingMode { get; set; }

        public int LocationX { get; set; }
        public int LocationY { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }

        public float RatioX { get; set; }
        public float RatioY { get; set; }
        public float RatioWidth { get; set; }
        public float RatioHeight { get; set; }


        // APPLICATION, BROWSER_TAB
        public string ProcessName { get; set; } = string.Empty;
        public string TitlePattern { get; set; } = string.Empty;
        public TitleMatchModeEnum TitleMatchMode { get; set; }
        public bool UseClientArea { get; set; } = true;


        // BROWSER_TAB
        public string TabMatchValue { get; set; } = string.Empty;
        public TabMatchOnEnum TabMatchOn { get; set; }


        // MONITOR
        public string MonitorUniqueId { get; set; } = string.Empty;


        public int FlowId { get; set; }

        // Read only, projected by the query.
        public int FlowStepsCount { get; set; }
        public string ParentName { get; set; } = string.Empty;
    }
}
