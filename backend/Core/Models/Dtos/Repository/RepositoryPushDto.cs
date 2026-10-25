namespace Core.Models.Dtos.Repository
{
    public class RepositoryPushDto
    {
        public string Branch { get; set; } = string.Empty;

        /// <summary>How many commits went. None when the remote had them all.</summary>
        public int CommitCount { get; set; }
    }
}
