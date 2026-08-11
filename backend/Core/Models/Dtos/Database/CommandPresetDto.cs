using Core.Enums;

namespace Core.Models.Dtos
{
    /// <summary>
    /// One entry of the preset catalog, sent to the form so it can render the picker and preview
    /// the command from the same definition the runner executes.
    /// </summary>
    public class CommandPresetDto
    {
        public RunCommandPresetEnum Preset { get; set; }
        public string Label { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public RunCommandShellEnum Shell { get; set; }

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
