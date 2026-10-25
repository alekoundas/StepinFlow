using Core.Enums;

namespace Core.Models.Business.Git
{
    /// <summary>A file that differs from the commit it is compared with.</summary>
    public sealed class GitFileChange
    {
        /// <summary>Relative to the repository's folder, with forward slashes.</summary>
        public string Path { get; set; } = string.Empty;
        public GitChangeKindEnum Kind { get; set; }
    }
}
