namespace Core.Models.Dtos.Repository
{
    /// <summary>
    /// Broadcast after flows were written to the repository or a commit was made, so what the
    /// app shows about it is read again.
    /// </summary>
    public class RepositoryChangedDto
    {
        public List<int> FlowIds { get; set; } = [];
    }
}
