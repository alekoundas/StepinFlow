namespace Core.Models.Business.Git
{
    public sealed class GitCommit
    {
        public string Sha { get; set; } = string.Empty;

        /// <summary>Null for the first commit.</summary>
        public string? ParentSha { get; set; }

        public string Message { get; set; } = string.Empty;
        public string AuthorName { get; set; } = string.Empty;
        public string AuthorEmail { get; set; } = string.Empty;
        public DateTimeOffset When { get; set; }
    }
}
