using Core.Models.Dtos.FlowScript;

namespace Core.Models.Dtos
{
    /// <summary>
    /// What a recording's draft saved, and what the Check page shows of it: the plain draft, the
    /// model's, or the plain one when the model's answer never read cleanly or the model could not
    /// be reached, and why. A revision asked for on the Check page answers with one too.
    /// </summary>
    public class FlowGenerationResultDto
    {
        public FlowScriptImportResultDto Import { get; set; } = new FlowScriptImportResultDto();

        /// <summary>True when the model's answer was saved.</summary>
        public bool IsModelDraft { get; set; }

        /// <summary>Why the model's answer was not saved. Empty when it was, and for a plain draft the tester chose.</summary>
        public string Reason { get; set; } = string.Empty;

        /// <summary>What the model says a tester should check.</summary>
        public List<string> Notes { get; set; } = [];

        /// <summary>The checks the model added, for the tester to keep or drop. In the saved flow until dropped.</summary>
        public List<ProposedCheckDto> ProposedChecks { get; set; } = [];

        /// <summary>Why the last answer was refused, when it was not saved because of it.</summary>
        public List<FlowScriptErrorDto> Refused { get; set; } = [];

        public int Attempts { get; set; }

        /// <summary>The first request's message, exactly as the model was sent it. The standing instructions are not in it.</summary>
        public string Request { get; set; } = string.Empty;

        /// <summary>The script the model was given, input values in: what it changed is told against this. Empty when no model was asked.</summary>
        public string Draft { get; set; } = string.Empty;

        /// <summary>The flow as saved, as script text. Empty when nothing was saved.</summary>
        public string Script { get; set; } = string.Empty;

        /// <summary>The saved flow's steps, without the Success and Failure branches that hold them.</summary>
        public int StepCount { get; set; }

        public int InputCount { get; set; }

        /// <summary>The saved flow's pictures, each once however many steps search for it.</summary>
        public int TemplateCount { get; set; }
    }
}
