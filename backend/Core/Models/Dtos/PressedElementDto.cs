namespace Core.Models.Dtos
{
    /// <summary>
    /// What a press landed on, as its application described it to Windows at that moment: a button,
    /// a link, a text field, with its name and state. None for an application that draws its own
    /// pixels, a game. The name and what it sits in are screen text.
    /// </summary>
    public class PressedElementDto
    {
        /// <summary>The click, drag or check it belongs to, by its index among the recording's actions.</summary>
        public int ActionIndex { get; set; }

        /// <summary>Button, link, text field, check box, in English.</summary>
        public string Role { get; set; } = string.Empty;

        public string Name { get; set; } = string.Empty;
        public bool IsEnabled { get; set; } = true;

        /// <summary>Ticked or not, for something that can be. Null for anything else.</summary>
        public bool? IsChecked { get; set; }

        /// <summary>A field whose text is hidden as it is typed.</summary>
        public bool IsPassword { get; set; }

        /// <summary>What it sits in, nearest first, each its role and name: document "Swag Labs".</summary>
        public List<string> Inside { get; set; } = [];
    }
}
