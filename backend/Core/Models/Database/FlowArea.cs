using Core.Enums;
using System.Collections.ObjectModel;

namespace Core.Models.Database
{
    /// <summary>
    /// A rectangle resolved at runtime, in physical pixels.
    ///
    /// A CUSTOM area may sit inside another area, which is what makes a flow portable: the flow
    /// resizes its own window, and everything inside is stored as an offset from that window
    /// rather than as a screen coordinate. Nesting is capped at one level.
    /// </summary>
    public class FlowArea : BaseDbModel
    {
        public string Name { get; set; } = string.Empty;
        public FlowAreaTypeEnum Type { get; set; }


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
        public int InstanceIndex { get; set; }

        /// <summary>Client area excludes the title bar and borders, which is nearly always wanted.</summary>
        public bool UseClientArea { get; set; } = true;


        // BROWSER_TAB
        public BrowserTypeEnum BrowserType { get; set; }
        public string TabMatchValue { get; set; } = string.Empty;
        public TabMatchOnEnum TabMatchOn { get; set; }


        // MONITOR
        public string MonitorUniqueId { get; set; } = string.Empty;


        // Flow
        public int FlowId { get; set; }
        public Flow Flow { get; set; } = null!;

        public IEnumerable<FlowArea> ChildFlowAreas { get; set; } = new Collection<FlowArea>();
        public IEnumerable<FlowPoint> FlowPoints { get; set; } = new Collection<FlowPoint>();
        public IEnumerable<FlowStep> FlowSteps { get; set; } = new Collection<FlowStep>();
    }
}
