namespace Core.Models.Dtos.Repository
{
    /// <summary>
    /// A branch switch as it goes, flow by flow: sent whole on every change, as
    /// REPOSITORY_BRANCH_SWITCH, and returned once it is done.
    /// </summary>
    public class RepositoryBranchSwitchDto
    {
        public string Branch { get; set; } = string.Empty;

        /// <summary>The flows hidden first, then the ones imported, sub-flows before their callers.</summary>
        public List<RepositoryBranchSwitchFlowDto> Flows { get; set; } = [];
    }
}
