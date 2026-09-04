namespace Core.Models.Dtos
{
    /// <summary>
    /// A finished recording in the shape a model can read.
    ///
    /// A recording is a long list of coordinates and a screenshot per click, which is far too much
    /// to hand a model - sixty screenshots is more tokens than a small model can hold at all. This
    /// is the same recording said in words, with the one measurement that cannot be inferred from
    /// words: which clicks hit the same thing in different places.
    /// </summary>
    public class RecordingSummaryDto
    {
        /// <summary>The window the recording happened in, when it stayed the same throughout.</summary>
        public string? WindowTitle { get; set; }

        public List<RecordedActionSummaryDto> Actions { get; set; } = new List<RecordedActionSummaryDto>();

        /// <summary>Distinct things that were clicked, each pointing back at the clicks that hit it.</summary>
        public List<ClickTargetDto> Targets { get; set; } = new List<ClickTargetDto>();
    }

    public class RecordedActionSummaryDto
    {
        public int Index { get; set; }
        public string Kind { get; set; } = string.Empty;
        public string Summary { get; set; } = string.Empty;

        /// <summary>Only ever set on a PAUSE, where it is the gap that was waited out.</summary>
        public int PauseMilliseconds { get; set; }

        /// <summary>Which entry in Targets this click landed on, when it hit something recognisable.</summary>
        public int? TargetIndex { get; set; }
    }

    /// <summary>
    /// One thing that was clicked, and everywhere it was clicked.
    ///
    /// A target clicked in more than one place is the whole point of this: it means the flow should
    /// look for it rather than click a fixed coordinate, and that is a fact measured from the
    /// screenshots rather than a guess anyone has to make.
    /// </summary>
    public class ClickTargetDto
    {
        public int Index { get; set; }

        /// <summary>The actions that clicked this same-looking thing, in order.</summary>
        public List<int> ActionIndexes { get; set; } = new List<int>();

        /// <summary>
        /// Clicked at more than one position, so it moves. A step for this wants an IMAGE_SEARCH
        /// and a click on the result, not a saved point.
        /// </summary>
        public bool IsMoving { get; set; }

        /// <summary>Where its picture is, for cropping a template image out of later.</summary>
        public int ScreenshotIndex { get; set; }
    }
}
