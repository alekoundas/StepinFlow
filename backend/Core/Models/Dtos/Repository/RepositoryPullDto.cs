namespace Core.Models.Dtos.Repository
{
    /// <summary>What a pull of the branch checked out would do, for the person to agree to first.</summary>
    public class RepositoryPullDto
    {
        public string Branch { get; set; } = string.Empty;

        /// <summary>The remote's commit the branch would be moved to. The pull is refused when the remote has moved since.</summary>
        public string RemoteSha { get; set; } = string.Empty;

        /// <summary>Newest first, the commits the remote has that the branch does not.</summary>
        public List<RepositoryCommitDto> Incoming { get; set; } = [];

        /// <summary>Newest first, the commits never pushed. They leave the branch and are kept on a backup branch.</summary>
        public List<RepositoryCommitDto> Dropped { get; set; } = [];
    }
}
