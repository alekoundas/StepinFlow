namespace Core.Models.Dtos.FlowScript
{
    

    /// <summary>
    /// What an import did, or why it did nothing. A failed import leaves the flow exactly as it
    /// was, so an error list and an untouched flow are the same outcome.
    /// </summary>
    public class FlowScriptImportResultDto
    {
        public bool IsSuccess { get; set; }

        public int FlowId { get; set; }
        public string FlowName { get; set; } = string.Empty;

        public int StepCount { get; set; }
        public int TemplateCount { get; set; }

        /// <summary>Templates the script named that were not in the folder beside it.</summary>
        public List<string> MissingTemplates { get; set; } = new List<string>();

        public List<FlowScriptErrorDto> Errors { get; set; } = new List<FlowScriptErrorDto>();

        /// <summary>What the file got away with, on an import that worked as well as one that did not.</summary>
        public List<FlowScriptErrorDto> Warnings { get; set; } = new List<FlowScriptErrorDto>();

        /// <summary>
        /// The imported flow as the validator sees it once saved. Errors here do not undo the
        /// import: the flow is saved, shows them, and will not run until they are fixed.
        /// </summary>
        public FlowValidationResultDto? Validation { get; set; }
    }
}
