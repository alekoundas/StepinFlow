namespace Core.Models.Dtos.Repository
{
    /// <summary>
    /// What one commit changed in one flow: its script before and after, and its images.
    /// </summary>
    public class RepositoryFlowCommitDto
    {
        public RepositoryCommitDto Commit { get; set; } = new RepositoryCommitDto();

        /// <summary>Null when the flow's file did not exist before this commit.</summary>
        public string? ScriptBefore { get; set; }

        /// <summary>Null when this commit removed it.</summary>
        public string? ScriptAfter { get; set; }

        public List<RepositoryImageChangeDto> Images { get; set; } = [];
    }
}
