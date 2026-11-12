using Business.Ai.AiDocuments;

namespace Business.Tests.Fakes
{
    /// <summary>
    /// An embedding model that turns text into the words it has of a few the test names, so how
    /// near two texts are is how many of those words they share. Unavailable unless a test says.
    /// </summary>
    public sealed class FakeEmbeddingService : IEmbeddingService
    {
        public bool IsReady { get; set; }

        public List<string> Words { get; } = [];

        public int Dimensions
        {
            get { return Words.Count; }
        }

        public bool IsAvailable()
        {
            return IsReady;
        }

        public string ModelFingerprint()
        {
            return "fake";
        }

        public float[] EmbedChunk(string text)
        {
            return Embed(text);
        }

        public float[] EmbedQuery(string text)
        {
            return Embed(text);
        }

        private float[] Embed(string text)
        {
            float[] vector = Words.Select(x => text.Contains(x, StringComparison.OrdinalIgnoreCase) ? 1f : 0f).ToArray();
            if (vector.All(x => x == 0f))
                vector = Words.Select(x => 0.01f).ToArray();

            return vector;
        }
    }
}
