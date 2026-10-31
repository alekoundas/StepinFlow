namespace Core.Models.Business
{
    /// <summary>
    /// An element as its application describes it to the system's accessibility interface - what a
    /// screen reader would say - rather than what it looks like.
    /// </summary>
    public sealed class AccessibleElement
    {
        /// <summary>What kind of thing it is, in English whatever the system's language: button, link, text field.</summary>
        public string Role { get; init; } = string.Empty;

        /// <summary>Its accessible name: its label, the text on it, a web page's aria-label. Screen text.</summary>
        public string Name { get; init; } = string.Empty;

        public bool IsEnabled { get; init; } = true;

        /// <summary>Ticked or not, for a check box, a toggle or a radio button. Null for anything else.</summary>
        public bool? IsChecked { get; init; }

        /// <summary>A field whose text is hidden as it is typed.</summary>
        public bool IsPassword { get; init; }

        /// <summary>What it sits in that has a name, nearest first: a group, a dialog, the page, the window.</summary>
        public IReadOnlyList<AccessibleElement> Inside { get; init; } = [];
    }
}
