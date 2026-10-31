using Core.Enums;
using Core.Models.Dtos.FlowScript;

namespace Core.Models.Dtos
{
    /// <summary>
    /// Where the model's draft of a reviewed recording is, for the recording page. Sent whole on
    /// every change, as FLOW_GENERATION_PROGRESS.
    /// </summary>
    public class FlowGenerationProgressDto
    {
        public FlowGenerationStateEnum State { get; set; }
        public FlowGenerationStageEnum Stage { get; set; }

        /// <summary>The model asked, as Settings names it.</summary>
        public string Model { get; set; } = string.Empty;

        /// <summary>Whether the text read under each click went to the model. Never to a cloud provider not allowed screen content.</summary>
        public bool IsReadTextSent { get; set; }

        /// <summary>The draft as a script, in characters, once written.</summary>
        public int ScriptLength { get; set; }

        /// <summary>How many of ToLookAt screens and wordless clicks the local model has looked at. Both 0 when it looks at none.</summary>
        public int LookedAt { get; set; }
        public int ToLookAt { get; set; }

        /// <summary>Which attempt, from 1, of AttemptCount. 0 before the model is asked.</summary>
        public int Attempt { get; set; }
        public int AttemptCount { get; set; }

        /// <summary>Why the last answer was refused, which the model is now asked to fix.</summary>
        public List<FlowScriptErrorDto> Refused { get; set; } = [];

        /// <summary>Set when nothing could be saved at all.</summary>
        public string? ErrorMessage { get; set; }
    }
}
