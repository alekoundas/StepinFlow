namespace Core.Models.Business.Git
{
    /// <summary>What a branch has that differs from the one checked out, flow by flow.</summary>
    public sealed class GitBranchSwitch
    {
        public string From { get; set; } = string.Empty;
        public string To { get; set; } = string.Empty;

        /// <summary>Each flow whose files differ, as the branch has it.</summary>
        public List<GitFlowFiles> Flows { get; set; } = [];

        /// <summary>The Id of each flow the branch does not have.</summary>
        public List<Guid> GoneFlowIds { get; set; } = [];
    }
}
