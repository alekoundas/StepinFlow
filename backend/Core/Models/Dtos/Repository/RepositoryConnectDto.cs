namespace Core.Models.Dtos.Repository
{
    /// <summary>What connecting found: an empty remote it sent the branches to, or one with history.</summary>
    public class RepositoryConnectDto
    {
        public string RemoteUrl { get; set; } = string.Empty;

        /// <summary>Every local branch, when the remote was empty and was sent them.</summary>
        public List<string> PushedBranches { get; set; } = [];

        /// <summary>What pulling the branch checked out would do, when the remote has that branch.</summary>
        public RepositoryPullDto? Pull { get; set; }
    }
}
