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

        /// <summary>Null when there is no remote.</summary>
        public string? RemoteUrl { get; set; }

        /// <summary>Whether the remote had the branch when it was last fetched.</summary>
        public bool IsBranchOnRemote { get; set; }

        /// <summary>Commits on the branch the remote does not have, and the other way round, as of the last fetch.</summary>
        public int AheadCount { get; set; }
        public int BehindCount { get; set; }
    }
}
