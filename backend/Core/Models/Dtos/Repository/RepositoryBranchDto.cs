namespace Core.Models.Dtos.Repository
{
    public class RepositoryBranchDto
    {
        public string Name { get; set; } = string.Empty;
        public bool IsCurrent { get; set; }

        /// <summary>Only on the remote: switching to it makes it here.</summary>
        public bool IsRemoteOnly { get; set; }
        public DateTimeOffset LastCommitAt { get; set; }
    }
}
