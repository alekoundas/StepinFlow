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

        // The window the flow works in: at most one, and always a root. A recording is stored
        // relative to it, and a viewport resizes it.
        public bool IsMain { get; set; }

        // What makes its contents bigger or smaller on another screen. Null inherits: a CUSTOM
        // child takes its parent's, and an area with no parent is DPI.
        public ScalesWithEnum? ScalesWith { get; set; }

        // The DPI its own offset and size were captured at, so an ABSOLUTE_PX child inside a DPI
        // area follows the monitor. A point carries its own.
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

        /// <summary>Client area excludes the title bar and borders, which is nearly always wanted.</summary>
        public bool UseClientArea { get; set; } = true;

        /// <summary>
        /// What a launch starts when the app is not open: a command line, or a link such as a Steam
        /// game's. Empty when nothing launches it.
        /// </summary>
        public string LaunchCommand { get; set; } = string.Empty;


        /// <summary>
        /// BROWSER_TAB: the page's address, which finds the tab and is where a launch opens the
        /// browser. A tab whose address starts with it is the one.
        /// </summary>
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
