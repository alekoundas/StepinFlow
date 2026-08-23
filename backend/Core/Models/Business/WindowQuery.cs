using Core.Enums;

namespace Core.Models.Business
{
    /// <summary>
    /// How to find a window. Title alone is too weak: titles mutate constantly and a substring
    /// match makes "Notepad" find "Notepad++".
    /// </summary>
    public class WindowQuery
    {
        public string ProcessName { get; set; } = string.Empty;
        public string TitlePattern { get; set; } = string.Empty;
        public TitleMatchModeEnum TitleMatchMode { get; set; }

        /// <summary>Measure inside the title bar and borders rather than the whole frame.</summary>
        public bool UseClientArea { get; set; } = true;
    }
}
