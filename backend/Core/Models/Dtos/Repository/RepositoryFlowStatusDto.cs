namespace Core.Models.Dtos.Repository
{
    public class RepositoryFlowStatusDto
    {
        public int FlowId { get; set; }
        public string FlowName { get; set; } = string.Empty;
        public int ChangeCount { get; set; }
    }
}
