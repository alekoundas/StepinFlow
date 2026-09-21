namespace Core.Models.Dtos
{
    /// <summary>Where a file stopped making sense, for the editor to point at.</summary>
    public class FlowScriptErrorDto
    {
        public int Line { get; set; }
        public int Column { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    /// <summary>
    /// What an import did, or why it did nothing. A failed import leaves the flow exactly as it
    /// was, so an error list and an untouched flow are the same outcome.
    /// </summary>
    public class FlowImportResultDto
    {
        public bool IsSuccess { get; set; }

        public int FlowId { get; set; }
        public string FlowName { get; set; } = string.Empty;

        public int StepCount { get; set; }
        public int TemplateCount { get; set; }

        /// <summary>Templates the script named that were not in the folder beside it.</summary>
        public List<string> MissingTemplates { get; set; } = new List<string>();

        public List<FlowScriptErrorDto> Errors { get; set; } = new List<FlowScriptErrorDto>();
    }
}
