namespace Core.Models.Dtos.FlowScript
{
    /// <summary>
    /// What an import meets: the flow it replaces, when one has the file's Id, and why it is
    /// refused, when another flow has the file's name.
    /// </summary>
    public class FlowScriptImportTargetDto
    {
        /// <summary>The flow with the file's Id. Null when the file is a new flow.</summary>
        public int? FlowId { get; set; }

        public string FlowName { get; set; } = string.Empty;

        /// <summary>The replaced flow's executions, which stay in its history.</summary>
        public int ExecutionCount { get; set; }

        /// <summary>Why the name cannot be used. Null when it is free.</summary>
        public string? NameTaken { get; set; }
    }
}
