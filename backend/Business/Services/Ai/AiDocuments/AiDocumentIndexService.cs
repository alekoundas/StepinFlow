using System.Security.Cryptography;
using System.Text;
using Cloud.Unum.USearch;
using Core.Helpers;
using Core.Models.Business;

namespace Business.Services.Ai.AiDocuments
{
    /// <summary>
    /// The shipped help, searchable by meaning.
    ///
    /// Only the vectors are stored. The chunks themselves are read back out of the assembly every
    /// time, so there is no second copy of the docs to fall out of step with the first - a key in
    /// the index is a position in <see cref="AiDocumentsReader.Read"/>, which is deterministic.
    ///
    /// The file is named after what was indexed, so an index built from different docs or a
    /// different model is not found rather than found and quietly wrong.
    /// </summary>
    public class AiDocumentIndexService : IAiDocumentIndexService, IDisposable
    {
        // Measured against these docs: an unrelated question tops out near 0.43 and a real one
        // starts around 0.65. Below the floor, saying nothing beats handing over the least bad chunk.
        private const float _minimumScore = 0.55f;
        private readonly IEmbeddingService _embeddingService;
        private readonly string _folder = PathHelper.GetAiDocumentsIndexDataPath();
        private readonly Lock _lockObj = new Lock();
        private bool _isBuildAttempted;

        private IReadOnlyList<AiDocumentChunk> _chunks = [];
        private USearchIndex? _index;

        public AiDocumentIndexService(IEmbeddingService embeddingService)
        {
            _embeddingService = embeddingService;
        }


        // ================================================================
        // Public methods
        // ================================================================
        /// <summary>
        /// Wakes up
        /// </summary>
        public bool IsAvailable()
        {
            Build();
            return _index != null;
        }

        public IReadOnlyList<AiDocumentSearchResult> Search(string question, int count)
        {
            if (string.IsNullOrWhiteSpace(question) || !IsAvailable())
                return [];

            float[] query = _embeddingService.EmbedQuery(question);
            _index!.Search(query, count, out ulong[] keys, out float[] distances);

            List<AiDocumentSearchResult> results = new List<AiDocumentSearchResult>();
            for (int i = 0; i < keys.Length; i++)
            {
                // Cosine distance between unit vectors, so the similarity is what is left of 1.
                float score = 1f - distances[i];

                if (score >= _minimumScore)
                    results.Add(new AiDocumentSearchResult
                    {
                        Chunk = _chunks[(int)keys[i]],
                        Score = score
                    });
            }

            return results;
        }

        public void Dispose()
        {
            _index?.Dispose();
            GC.SuppressFinalize(this);
        }

        // ================================================================
        // Private methods
        // ================================================================

        // Embeds every chunk for the first ever run, then loads existing file.
        private void Build()
        {
            if (_isBuildAttempted)
                return;

            lock (_lockObj)
            {
                if (_isBuildAttempted)
                    return;

                try
                {
                    if (!_embeddingService.IsAvailable())
                        return;

                    _chunks = AiDocumentsReader.Read();

                    if (_chunks.Count == 0)
                        return;

                    string path = Path.Combine(PathHelper.GetAiDocumentsIndexDataPath(), $"aidocuments-{Fingerprint()}.usearch");
                    if (!IsFileValid(path))
                    {
                        _index = new USearchIndex(
                            MetricKind.Cos,
                            ScalarKind.Float32,
                            (ulong)_embeddingService.Dimensions,
                            connectivity: 0,
                            expansionAdd: 0,
                            expansionSearch: 0,
                            multi: false);

                        for (int i = 0; i < _chunks.Count; i++)
                            _index.Add((ulong)i, _embeddingService.EmbedChunk(_chunks[i].Text)); // Embed and add to index.

                        _index.Save(path);
                    }

                    // Also after a load: the run that leaves an old index behind is the one that did not have to rebuild.
                    Forget(path);
                }
                finally
                {
                    _isBuildAttempted = true;
                }
            }
        }

        // A file under the right name can still be half written, from a build that was killed while saving, so the count is checked rather than trusted.
        private bool IsFileValid(string path)
        {
            if (!File.Exists(path))
                return false;

            try
            {
                _index = new USearchIndex(path, view: false);

                if ((int)_index.Size() == _chunks.Count)
                    return true;
            }
            catch (Exception) { }

            _index?.Dispose();
            _index = null;

            return false;
        }

        // Indexes for docs or a model this build no longer has.
        private void Forget(string path)
        {
            foreach (string file in Directory.EnumerateFiles(_folder, "aidocuments-*.usearch"))
            {
                if (string.Equals(file, path, StringComparison.OrdinalIgnoreCase))
                    continue;

                try
                {
                    File.Delete(file);
                }
                catch (IOException) { }
            }
        }

        // Everything the vectors depend on: the text that was embedded, and the model that embedded it.
        private string Fingerprint()
        {
            StringBuilder builder = new StringBuilder(_embeddingService.ModelFingerprint());

            foreach (AiDocumentChunk chunk in _chunks)
                builder.Append(chunk.Text);

            byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes(builder.ToString()));

            return Convert.ToHexString(hash)[..16].ToLowerInvariant();
        }
    }
}
