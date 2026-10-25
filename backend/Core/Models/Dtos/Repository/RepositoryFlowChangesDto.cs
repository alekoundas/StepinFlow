namespace Core.Models.Dtos.Repository
{
    /// <summary>
    /// One flow's files against the last commit: its script then and now, and its images.
    /// </summary>
    public class RepositoryFlowChangesDto
    {
        public int FlowId { get; set; }
        public string ScriptPath { get; set; } = string.Empty;
        public int ChangeCount { get; set; }

        /// <summary>The file as it is on disk now.</summary>
        public string Script { get; set; } = string.Empty;

        /// <summary>Null when the last commit has no such file.</summary>
        public string? CommittedScript { get; set; }

        public List<RepositoryImageChangeDto> Images { get; set; } = [];
    }
}
