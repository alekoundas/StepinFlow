namespace Core.Models.Business.Git
{
    /// <summary>A local branch and the commit it points at.</summary>
    public sealed class GitBranch
    {
        public string Name { get; set; } = string.Empty;
        public string Sha { get; set; } = string.Empty;
        public bool IsCurrent { get; set; }

        /// <summary>When its last commit was made.</summary>
        public DateTimeOffset When { get; set; }
    }
}
