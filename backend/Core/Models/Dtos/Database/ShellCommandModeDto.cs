using Core.Enums;

namespace Core.Models.Dtos
{
    /// <summary>
    /// One entry of the mode catalog, sent to the form so it can render the picker and preview
    /// the command from the same definition the executor uses.
    /// </summary>
    public class ShellCommandModeDto
    {
        public ShellCommandModeEnum Mode { get; set; }
        public string Label { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public ShellTypeEnum ShellType { get; set; }

        /// <summary>{0} is the parameter, when there is one.</summary>
        public string CommandTemplate { get; set; } = string.Empty;

        public bool HasParameter { get; set; }
        public string ParameterLabel { get; set; } = string.Empty;
        public string ParameterPlaceholder { get; set; } = string.Empty;
        public string ParameterDefault { get; set; } = string.Empty;

        /// <summary>CUSTOM only. Everything else is edited through its parameter.</summary>
        public bool IsEditable { get; set; }

        /// <summary>Testing this for real does something the user may not want. Ask first.</summary>
        public bool IsConfirmationRequired { get; set; }
    }
}
