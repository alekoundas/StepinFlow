using System.Numerics.Tensors;
using Core.Enums;
using Core.Models.Database;

namespace Business.Ai.AiDocuments
{
    /// <summary>
    /// The example flows, searchable by meaning: each is embedded once, by its title and what it
    /// is for, with the embedding model the assistant's documents use, and the one nearest a flow's
    /// name, description and window goes with its request. Every document in AiDocuments/Examples
    /// is one, so adding an example is adding a file, and a store's most used flows could be
    /// indexed the same way.
    ///
    /// The vectors belong to the embedding model, not to the model that answers: changing that one
    /// changes nothing here. A handful of examples is held in memory; a store's worth would want
    /// the file the document index keeps.
    /// </summary>
    public sealed class FlowExampleIndex : IFlowExampleIndex
    {
        // A description of a word or two says nothing to match by.
        private const int MINIMUM_WORDS = 3;

        // Below it, the nearest example is not near: the rule picks.
        private const float MINIMUM_SCORE = 0.55f;

        private const string EXAMPLES_FOLDER = "Examples";

        private readonly IEmbeddingService _embeddingService;
        private readonly Lock _lockObj = new Lock();
        private List<IndexedExample>? _examples;

        public FlowExampleIndex(IEmbeddingService embeddingService)
        {
            _embeddingService = embeddingService;
        }


        // ================================================================
        // Public methods
        // ================================================================

        public string DocumentFor(Flow flow, FlowArea? mainArea)
        {
            string rule = FlowExamples.DocumentFor(mainArea);

            int words = flow.Description.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).Length;
            if (words < MINIMUM_WORDS || !_embeddingService.IsAvailable())
                return rule;

            List<IndexedExample> examples = Examples();
            if (examples.Count == 0)
                return rule;

            float[] query = _embeddingService.EmbedQuery($"{flow.Name}. {flow.Description} {WindowOf(mainArea)}");
            IndexedExample nearest = examples.MaxBy(x => TensorPrimitives.CosineSimilarity(x.Vector, query))!;

            if (TensorPrimitives.CosineSimilarity(nearest.Vector, query) < MINIMUM_SCORE)
                return rule;

            return nearest.Document;
        }


        // ================================================================
        // Private methods
        // ================================================================

        private List<IndexedExample> Examples()
        {
            lock (_lockObj)
            {
                if (_examples != null)
                    return _examples;

                List<IndexedExample> examples = new List<IndexedExample>();
                foreach (string document in AiDocumentsReader.DocumentNamesIn(EXAMPLES_FOLDER))
                {
                    string about = AboutOf(AiDocumentsReader.ReadDocument(document));
                    if (about.Length > 0)
                        examples.Add(new IndexedExample(document, _embeddingService.EmbedChunk(about)));
                }

                _examples = examples;
                return examples;
            }
        }

        // Its title and what it is for: everything before its first section.
        private static string AboutOf(string document)
        {
            List<string> lines = document.Replace("\r\n", "\n").Split('\n').ToList();
            int section = lines.FindIndex(1, x => x.StartsWith("## ", StringComparison.Ordinal));
            if (section < 0)
                section = lines.Count;

            IEnumerable<string> about = lines.Take(section).Select(x => x.TrimStart('#').Trim()).Where(x => x.Length > 0);
            return string.Join(' ', about);
        }

        // What kind of window, in words an example's text has: a website, a game.
        private static string WindowOf(FlowArea? mainArea)
        {
            if (mainArea == null)
                return string.Empty;

            if (mainArea.Type == FlowAreaTypeEnum.BROWSER_TAB)
                return $"A website, {mainArea.TabAddress}, in a browser.";

            if (mainArea.ScalesWith == ScalesWithEnum.AREA)
                return $"A game, {mainArea.ProcessName}, that draws its own pixels.";

            return $"An application, {mainArea.ProcessName}.";
        }


        // ================================================================
        // Private types
        // ================================================================

        private sealed record IndexedExample(string Document, float[] Vector);
    }
}
