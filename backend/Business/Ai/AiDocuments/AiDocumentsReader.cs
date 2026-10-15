using System.Reflection;
using Core.Models.Business;

namespace Business.Ai.AiDocuments
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

        /// <summary>One document whole, by its file name, wherever in the folders it sits.</summary>
        public static string ReadDocument(string fileName)
        {
            Assembly assembly = typeof(AiDocumentChunk).Assembly;
            string suffix = $".{fileName}";

            string name = assembly.GetManifestResourceNames()
                .FirstOrDefault(x => x.StartsWith(_prefix, StringComparison.Ordinal) && x.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
                ?? throw new InvalidOperationException($"{fileName} is not among the AI documents.");

            using Stream stream = assembly.GetManifestResourceStream(name)
                ?? throw new InvalidOperationException($"{fileName} could not be read.");
            using StreamReader reader = new StreamReader(stream);

            return reader.ReadToEnd();
        }
    }
}
