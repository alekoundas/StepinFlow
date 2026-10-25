namespace Core.Models.Dtos.Repository
{
    public class RepositoryConnectRequestDto
    {
        public string Url { get; set; } = string.Empty;

        /// <summary>Empty keeps the token already saved.</summary>
        public string Token { get; set; } = string.Empty;
    }
}
