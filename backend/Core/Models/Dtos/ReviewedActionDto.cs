using Core.Enums;

namespace Core.Models.Dtos
{
    /// <summary>What the tester decided about one recorded action, in Review.</summary>
    public class ReviewedActionDto
    {
        public int ActionIndex { get; set; }
        public ReviewChoiceEnum Choice { get; set; }

        // The element's rectangle as reviewed, in the main area's pixels. Null for an action with none.
        // Searched by its picture, the template is cut along it; by its words, they are read inside it.
        public int? ElementX { get; set; }
        public int? ElementY { get; set; }
        public int? ElementWidth { get; set; }
        public int? ElementHeight { get; set; }

        public SearchByEnum SearchBy { get; set; }

        /// <summary>For a search by its words: the words it waits to read.</summary>
        public string? SearchText { get; set; }

        /// <summary>For typed text that becomes an input, or an input's value typed: its name.</summary>
        public string? InputName { get; set; }

        /// <summary>For an input: never written to a file, never shown to a model.</summary>
        public bool IsSecret { get; set; }

        /// <summary>For a command: what it executes. For an application: the program that opens it.</summary>
        public string? CommandText { get; set; }
    }
}
