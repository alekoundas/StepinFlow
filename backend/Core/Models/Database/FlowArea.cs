using Core.Enums;
using System.Collections.ObjectModel;

namespace Core.Models.Database
{
    /// <summary>
    /// A rectangle resolved at runtime, in physical pixels.
    /// </summary>
    public class FlowArea : BaseDbModel
    {
        public string Name { get; set; } = string.Empty;
        public FlowAreaTypeEnum Type { get; set; }

        // The window the flow works in: at most one, and always a root.
        public bool IsMain { get; set; }
        public ScalesWithEnum? ScalesWith { get; set; }

        // The DPI its own offset and size were captured at, so an ABSOLUTE_PX child inside a DPI area follows the monitor.
        public int AuthoredDpi { get; set; }


        // CUSTOM
        public int? ParentFlowAreaId { get; set; }
        public FlowArea? ParentFlowArea { get; set; }

        public AreaSizingModeEnum SizingMode { get; set; }

        // ABSOLUTE_PX: pixels, relative to the parent when there is one.
        public int LocationX { get; set; }
        public int LocationY { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }

        // RATIO: 0..1 of the parent.
        public float RatioX { get; set; }
        public float RatioY { get; set; }
        public float RatioWidth { get; set; }
        public float RatioHeight { get; set; }


        // APPLICATION, BROWSER_TAB
        public string ProcessName { get; set; } = string.Empty;
        public string TitlePattern { get; set; } = string.Empty;
        public TitleMatchModeEnum TitleMatchMode { get; set; }
        public bool IgnoreWindowBorders { get; set; } = true;


        // APPLICATION
        public string LaunchCommand { get; set; } = string.Empty;

        // BROWSER_TAB
        public string TabAddress { get; set; } = string.Empty;


        // MONITOR. The GDI device name - \\.\DISPLAY1.  Empty is the primary monitor, the portable choice.
        public string MonitorDeviceName { get; set; } = string.Empty;


        // Flow
        public int FlowId { get; set; }
        public Flow Flow { get; set; } = null!;


        public IEnumerable<FlowArea> ChildFlowAreas { get; set; } = new Collection<FlowArea>();
        public IEnumerable<FlowPoint> FlowPoints { get; set; } = new Collection<FlowPoint>();
        public IEnumerable<FlowStep> FlowSteps { get; set; } = new Collection<FlowStep>();
    }
}
