using Core.Enums;

namespace Core.Models.Dtos
{
    public class FlowAreaDto
    {
        /// <summary>0 for a new row. Negative for a new row another new row points at.</summary>
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;
        public FlowAreaTypeEnum Type { get; set; }
        public bool IsMain { get; set; }

        public ScalesWithEnum? ScalesWith { get; set; }
        public int AuthoredDpi { get; set; }


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
        public bool IgnoreWindowBorders { get; set; } = true;
        public string LaunchCommand { get; set; } = string.Empty;


        // BROWSER_TAB
        public string TabAddress { get; set; } = string.Empty;


        // MONITOR. Empty is the primary monitor.
        public string MonitorDeviceName { get; set; } = string.Empty;


        public int FlowId { get; set; }

        // Read only, projected by the query.
        public int FlowStepsCount { get; set; }
        public string ParentName { get; set; } = string.Empty;
    }
}
