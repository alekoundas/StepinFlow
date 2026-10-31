namespace Core.Models.Dtos
{
    /// <summary>
    /// A check a model added to a recording, which the Check page offers to keep or drop: one that
    /// nothing moves to, and that the script it was given did not have.
    /// </summary>
    public class ProposedCheckDto
    {
        public string Name { get; set; } = string.Empty;

        /// <summary>Its line in the saved script, from 1. 0 when it could not be found.</summary>
        public int Line { get; set; }

        /// <summary>That line, as the script has it.</summary>
        public string Text { get; set; } = string.Empty;

        /// <summary>The step whose branch it sits in. Empty at the top of the flow.</summary>
        public string Under { get; set; } = string.Empty;

        /// <summary>
        /// Whether a line OCR read on the recording's screenshots satisfies its words. Null for a
        /// check of a picture, or of words an execution fills in.
        /// </summary>
        public bool? IsReadOnScreen { get; set; }
    }
}
