namespace Core.Models.Dtos.Repository
{
    public class RepositoryCommitDto
    {
        public string Sha { get; set; } = string.Empty;
        public string ShortSha { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string AuthorName { get; set; } = string.Empty;
        public DateTimeOffset When { get; set; }
    }
}
