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

        /// <summary>Which one when several match, in z-order.</summary>
        public int InstanceIndex { get; set; }

        public bool UseClientArea { get; set; } = true;
    }
}
