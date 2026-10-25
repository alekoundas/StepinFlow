namespace Core.Models.Business.Git
{
    /// <summary>The commit a flow was at, and whether its files, or a sub-flow's, had changes not committed yet.</summary>
    public sealed class GitFlowVersion
    {
        /// <summary>Null while the repository has no commits.</summary>
        public string? CommitSha { get; set; }

        public bool HasUncommittedChanges { get; set; }
    }
}
