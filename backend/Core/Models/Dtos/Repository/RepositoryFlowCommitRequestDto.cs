namespace Core.Models.Dtos.Repository
{
    public class RepositoryFlowCommitRequestDto
    {
        public int FlowId { get; set; }
        public string Sha { get; set; } = string.Empty;
    }
}
