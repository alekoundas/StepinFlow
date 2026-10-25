namespace Core.Models.Business.Git
{
    /// <summary>A flow's script and template images as one commit holds them.</summary>
    public sealed class GitFlowFiles
    {
        public string Script { get; set; } = string.Empty;

        /// <summary>By file name, as an import looks them up.</summary>
        public Dictionary<string, byte[]> Templates { get; } = new Dictionary<string, byte[]>(StringComparer.OrdinalIgnoreCase);
    }
}
