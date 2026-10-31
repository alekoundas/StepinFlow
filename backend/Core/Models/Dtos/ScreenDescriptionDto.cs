namespace Core.Models.Dtos
{
    /// <summary>
    /// What the local model saw at one press, asked as a reviewed recording goes to a model: a
    /// sentence for the page where one began, a few words for a click nothing else named. Written
    /// from the screen, so screen text.
    /// </summary>
    public class ScreenDescriptionDto
    {
        /// <summary>The press's action, by its index among the recording's actions.</summary>
        public int ActionIndex { get; set; }

        /// <summary>Which page, screen or dialog it is, in a sentence. Null where no page began.</summary>
        public string? Page { get; set; }

        /// <summary>What was pressed, in a few words, when neither OCR nor Windows named it.</summary>
        public string? Element { get; set; }
    }
}
