using Business.FlowScript.Diagnostics;

namespace Business.Ai.ScriptRevision.Models
{
    /// <summary>
    /// What came of asking a model to revise a script: the revised script, or why there is none.
    /// </summary>
    public sealed class ScriptRevisionResult
    {
        /// <summary>
        /// The answer that read cleanly and kept to the rules, with what the code owns put back in
        /// it. Null when there was none.
        /// </summary>
        public string? Script { get; set; }

        /// <summary>What the model says a person should check.</summary>
        public List<string> Notes { get; set; } = [];

        /// <summary>Old input name to new.</summary>
        public Dictionary<string, string> InputRenames { get; set; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        /// <summary>Why the last answer was refused. Empty once one was taken.</summary>
        public List<Diagnostic> Refused { get; set; } = [];

        public int Attempts { get; set; }

        /// <summary>Set when no answer could be had at all: no model set up, or none reachable.</summary>
        public string? ErrorMessage { get; set; }

        /// <summary>The first request's message, as the model was sent it.</summary>
        public string Request { get; set; } = string.Empty;

        public bool IsRevised
        {
            get { return Script != null; }
        }
    }
}
