using System.Reflection;
using Core.Models.Business;

namespace Business.Services.Ai.AiDocuments
{
    /// <summary>
    /// The shipped Ai documents.
    /// </summary>
    public static class AiDocumentsReader
    {
        // Find  "Core" and get .md files.
        private static readonly string _prefix = $"{typeof(AiDocumentChunk).Assembly.GetName().Name}.AiDocuments.";

        public static IReadOnlyList<AiDocumentChunk> Read()
        {
            Assembly assembly = typeof(AiDocumentChunk).Assembly;
            List<AiDocumentChunk> chunks = new List<AiDocumentChunk>();

            // Enumerated rather than named. Resource names are built from the folder layout, so
            // spelling one out here would break the next time a file moves.
            foreach (string name in assembly.GetManifestResourceNames().OrderBy(x => x))
            {
                if (!name.StartsWith(_prefix, StringComparison.Ordinal) || !name.EndsWith(".md", StringComparison.OrdinalIgnoreCase))
                    continue;

                using Stream? stream = assembly.GetManifestResourceStream(name);
                if (stream == null)
                    continue;

                using StreamReader reader = new StreamReader(stream);
                chunks.AddRange(AiDocumentChunker.Split(reader.ReadToEnd(), name));
            }

            return chunks;
        }
    }
}
