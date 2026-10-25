using Core.Enums;

namespace Core.Models.Dtos.Repository
{
    /// <summary>
    /// A file not committed yet, as the commit dialog lists it.
    /// </summary>
    public class RepositoryFileChangeDto
    {
        public string Path { get; set; } = string.Empty;
        public GitChangeKindEnum Kind { get; set; }

        /// <summary>Null once the flow is gone, when only its files are left to commit.</summary>
        public int? FlowId { get; set; }

        public string FlowName { get; set; } = string.Empty;
        public bool IsImage { get; set; }

        /// <summary>A template image as it is now, or as it was when it is the one deleted.</summary>
        public byte[]? Image { get; set; }
    }
}
