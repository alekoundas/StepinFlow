using Core.Enums;

namespace Core.Models.Dtos
{
    /// <summary>
    /// One thing the user did, after a press and a release have been folded into a click and a
    /// burst of typing into a single entry.
    ///
    /// Deliberately not a step. What a click should become is the wizard question, and answering
    /// it here would be guessing on the user behalf. This carries the evidence and the raw facts
    /// a step needs, and nothing about which step that is.
    /// </summary>
    public class RecordedActionDto
    {
        public int Index { get; set; }
        public RecordedActionKindEnum Kind { get; set; }

        /// <summary>One line describing what happened, shown above the choices.</summary>
        public string Summary { get; set; } = string.Empty;

        public string? WindowTitle { get; set; }

        /// <summary>Key into the session screenshot store, when one was captured.</summary>
        public int? ScreenshotIndex { get; set; }

        // Cursor
        public int LocationX { get; set; }
        public int LocationY { get; set; }
        public int LocationEndX { get; set; }
        public int LocationEndY { get; set; }
        public CursorButtonTypeEnum? CursorButtonType { get; set; }

        /// <summary>
        /// Single or double, so a double click is offered as one rather than as two clicks the
        /// flow would replay too slowly to register as one.
        /// </summary>
        public CursorButtonActionTypeEnum? CursorButtonActionType { get; set; }

        // Scroll
        public CursorScrollDirectionTypeEnum? ScrollDirection { get; set; }
        public int ScrollAmount { get; set; }

        // Keyboard: the typed run, or the name of the key that was pressed.
        public string? Text { get; set; }

        /// <summary>
        /// How many times the keyboard repeated the key while it was held. Zero for an ordinary
        /// press. The repeats are not replayed - a flow that types forty a's because a finger
        /// lingered is worse than one that types a single a - so this is what was left out.
        /// </summary>
        public int RepeatCount { get; set; }

        /// <summary>How long the key was down, which is the part a hold is actually about.</summary>
        public int HoldMilliseconds { get; set; }

        // Pause
        public int PauseMilliseconds { get; set; }
    }
}
