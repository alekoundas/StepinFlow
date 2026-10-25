namespace Core.Models.Business.Git
{
    /// <summary>Who a commit is by, and when.</summary>
    public sealed class GitSignature
    {
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public DateTimeOffset When { get; set; }
    }
}
