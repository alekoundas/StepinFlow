namespace Business.Services.Ai.AiDocuments
{
    public interface IEmbeddingService
    {
        int Dimensions { get; }
        bool IsAvailable();
        string ModelFingerprint();
        float[] EmbedChunk(string text);
        float[] EmbedQuery(string text);
    }
}
