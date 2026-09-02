namespace Core.Models.Business
{
    /// <summary>
    /// One chunk the index matched, and how well.
    ///
    /// The score is cosine similarity between unit vectors, so it runs 0 to 1 and is comparable
    /// across questions - which is what makes a relevance floor possible at all.
    /// </summary>
    public class AiDocumentSearchResult
    {
        public AiDocumentChunk Chunk { get; set; } = null!;
        public float Score { get; set; }
    }
}
