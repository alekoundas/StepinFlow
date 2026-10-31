namespace Core.Models.Dtos
{
    /// <summary>
    /// What changed on the main area between one press's screenshot and the next, by the text OCR
    /// read: what the action, and any up to the next press, made appear and go. The text is screen
    /// data and reaches a cloud model only through the screen data gate; how much changed is a
    /// number and goes anywhere.
    /// </summary>
    public class ScreenChangeDto
    {
        /// <summary>The action whose press the change is measured from, by its index.</summary>
        public int ActionIndex { get; set; }

        /// <summary>The next action with a press, whose screenshot it is measured to. Null for the screen the recording stopped on.</summary>
        public int? NextActionIndex { get; set; }

        /// <summary>Lines read on the second screenshot and not the first, in the order read.</summary>
        public List<string> Appeared { get; set; } = [];

        /// <summary>Lines read on the first screenshot and not the second.</summary>
        public List<string> Gone { get; set; } = [];

        /// <summary>How much of the text changed, from 0 to 1.</summary>
        public double ChangedShare { get; set; }

        /// <summary>Most of the text changed: a new page or a dialog.</summary>
        public bool IsNewScreen { get; set; }

        /// <summary>The main area's window has another title at the second press: for a browser, another page.</summary>
        public bool IsNewTitle { get; set; }

        /// <summary>The window's title at the second press, when it was pressed in the main area. Screen text.</summary>
        public string? Title { get; set; }
    }
}
