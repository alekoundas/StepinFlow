using Core.Enums;

namespace Core.Models.Dtos.Repository
{
    /// <summary>One flow a branch switch touches.</summary>
    public class RepositoryBranchSwitchFlowDto
    {
        /// <summary>Null for a flow new on the branch until it is imported.</summary>
        public int? FlowId { get; set; }

        public string Name { get; set; } = string.Empty;
        public bool IsSubFlow { get; set; }

        /// <summary>New on the branch, changed on it, or not on it - hidden.</summary>
        public GitChangeKindEnum Change { get; set; }

        public bool IsDone { get; set; }
    }
}
