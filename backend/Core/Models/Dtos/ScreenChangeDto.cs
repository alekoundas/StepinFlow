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

        /// <summary>The next action with a press, whose screenshot it is measured to.</summary>
        public int NextActionIndex { get; set; }

        /// <summary>Lines read on the second screenshot and not the first, in the order read.</summary>
        public List<string> Appeared { get; set; } = [];

        /// <summary>Lines read on the first screenshot and not the second.</summary>
        public List<string> Gone { get; set; } = [];

        /// <summary>How much of the text changed, from 0 to 1.</summary>
        public double ChangedShare { get; set; }

        /// <summary>Most of the text changed: a new page or a dialog.</summary>
        public bool IsNewScreen { get; set; }
    }
}
