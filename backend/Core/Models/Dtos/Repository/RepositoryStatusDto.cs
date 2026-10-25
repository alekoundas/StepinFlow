namespace Core.Models.Dtos.Repository
{
    /// <summary>
    /// Where the repository is, the branch, and which flows have changes not committed yet.
    /// </summary>
    public class RepositoryStatusDto
    {
        public string FolderPath { get; set; } = string.Empty;
        public string Branch { get; set; } = string.Empty;
        public int CommitCount { get; set; }
        public DateTimeOffset? LastCommitAt { get; set; }

        /// <summary>Every changed file, whichever flow it belongs to.</summary>
        public int ChangeCount { get; set; }

        /// <summary>Only the flows with changes.</summary>
        public List<RepositoryFlowStatusDto> Flows { get; set; } = [];

        /// <summary>What the user's git config says, shown where the Settings fields are left empty.</summary>
        public string ConfiguredAuthorName { get; set; } = string.Empty;
        public string ConfiguredAuthorEmail { get; set; } = string.Empty;
    }
}
