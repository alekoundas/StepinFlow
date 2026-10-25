namespace Core.Models.Dtos.Repository
{
    public class RepositoryBranchDto
    {
        public string Name { get; set; } = string.Empty;
        public bool IsCurrent { get; set; }
        public DateTimeOffset LastCommitAt { get; set; }
    }
}
