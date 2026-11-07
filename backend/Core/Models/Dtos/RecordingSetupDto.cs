using Core.Enums;

namespace Core.Models.Dtos
{
    /// <summary>What the recording setup asks before the first click: the flow, and the window it works in.</summary>
    public class RecordingSetupDto
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public bool IsSubFlow { get; set; }
        public bool IsQaTest { get; set; }

        // The main area: an application's window, or a website's tab in a browser.
        public FlowAreaTypeEnum AreaType { get; set; } = FlowAreaTypeEnum.APPLICATION;
        public string ProcessName { get; set; } = string.Empty;
        public string TitlePattern { get; set; } = string.Empty;
        public TitleMatchModeEnum TitleMatchMode { get; set; }
        public string TabAddress { get; set; } = string.Empty;
        public bool IgnoreWindowBorders { get; set; } = true;
        public ScalesWithEnum? ScalesWith { get; set; }

        /// <summary>Whether the flow starts by launching the application.</summary>
        public bool AddsLaunchStep { get; set; }

        /// <summary>Whether the flow then sets its window to <see cref="WindowWidth"/> by <see cref="WindowHeight"/>, title bar and borders included.</summary>
        public bool AddsResizeStep { get; set; }
        public int WindowWidth { get; set; }
        public int WindowHeight { get; set; }
    }
}
