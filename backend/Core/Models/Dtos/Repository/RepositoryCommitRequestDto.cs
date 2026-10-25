namespace Core.Models.Dtos.Repository
{
    public class RepositoryCommitRequestDto
    {
        public string Message { get; set; } = string.Empty;

        /// <summary>The files to commit, as the changes list names them. Nothing else goes in.</summary>
        public List<string> Paths { get; set; } = [];
    }
}
