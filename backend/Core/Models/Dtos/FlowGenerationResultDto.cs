using Core.Models.Dtos.FlowScript;

namespace Core.Models.Dtos
{
    /// <summary>
    /// What a generation saved: the model's draft, or the plain one when the model's answer never
    /// read cleanly or the model could not be reached, and why.
    /// </summary>
    public class FlowGenerationResultDto
    {
        public FlowScriptImportResultDto Import { get; set; } = new FlowScriptImportResultDto();

        /// <summary>True when the model's answer was saved, false when the plain draft was.</summary>
        public bool IsModelDraft { get; set; }

        /// <summary>Why the plain draft was saved instead. Empty for the model's draft.</summary>
        public string Reason { get; set; } = string.Empty;

        /// <summary>What the model says a tester should check.</summary>
        public List<string> Notes { get; set; } = [];

        /// <summary>Why the last answer was refused, when the plain draft was saved because of it.</summary>
        public List<FlowScriptErrorDto> Refused { get; set; } = [];

        public int Attempts { get; set; }

        /// <summary>The first request's message, exactly as the model was sent it. The standing instructions are not in it.</summary>
        public string Request { get; set; } = string.Empty;
    }
}
