using Core.Enums;

namespace Core.Models.Dtos
{
    /// <summary>What the tester decided about one recorded action, in Review.</summary>
    public class ReviewedActionDto
    {
        public int ActionIndex { get; set; }
        public ReviewChoiceEnum Choice { get; set; }

        // The element's rectangle as reviewed, in the main area's pixels. Null for an action with none.
        public int? ElementX { get; set; }
        public int? ElementY { get; set; }
        public int? ElementWidth { get; set; }
        public int? ElementHeight { get; set; }

        /// <summary>For typed text that becomes an input: its name.</summary>
        public string? InputName { get; set; }

        /// <summary>For an input: never written to a file, never shown to a model.</summary>
        public bool IsSecret { get; set; }
    }
}
