using Core.Enums;

namespace Core.Models.Dtos.Repository
{
    /// <summary>
    /// A template image that changed, before and after.
    /// </summary>
    public class RepositoryImageChangeDto
    {
        public string Name { get; set; } = string.Empty;
        public GitChangeKindEnum Kind { get; set; }

        /// <summary>Null when it was added.</summary>
        public byte[]? Before { get; set; }

        /// <summary>Null when it was deleted.</summary>
        public byte[]? After { get; set; }
    }
}
