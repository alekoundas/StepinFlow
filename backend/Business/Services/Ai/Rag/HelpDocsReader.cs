using System.Reflection;

using Core.Models.Business;

namespace Business.Services.Ai.Rag
{
    /// <summary>
    /// The shipped help, in chunks.
    ///
    /// Read from the assembly rather than from disk: the docs are compiled into Core, so there is
    /// no folder to find, nothing to copy per platform, and no way for an install to be missing
    /// the pages the assistant answers from.
    /// </summary>
    public static class HelpDocsReader
    {
        private const string _extension = ".md";

        public static IReadOnlyList<DocumentChunk> Read()
        {
            Assembly assembly = typeof(DocumentChunk).Assembly;

            List<DocumentChunk> chunks = new List<DocumentChunk>();

            // Enumerated rather than named. Resource names are built from the folder layout, so
            // spelling one out here would break the next time a file moves.
            foreach (string name in assembly.GetManifestResourceNames().OrderBy(x => x))
            {
                if (!name.EndsWith(_extension, StringComparison.OrdinalIgnoreCase))
                    continue;

                using Stream? stream = assembly.GetManifestResourceStream(name);
                if (stream == null)
                    continue;

                using StreamReader reader = new StreamReader(stream);
                chunks.AddRange(HelpDocChunker.Split(reader.ReadToEnd(), name));
            }

            return chunks;
        }
    }
}
