using System.ComponentModel;
using Business.Services.Ai.AiDocuments;

namespace Business.Services.Ai.Tools
{
    /// <summary>
    /// What the model may ask the help.
    ///
    /// only the docs say how the app works, and a model guessing at that is the failure this is here to prevent.
    /// </summary>
    public sealed class AiDocumentTools
    {
        private const int _maxResults = 5; // Max cap on loops

        private readonly IAiDocumentIndexService _aiDocumentIndexService;

        public AiDocumentTools(IAiDocumentIndexService aiDocumentIndexService)
        {
            _aiDocumentIndexService = aiDocumentIndexService;
        }


        // ================================================================
        // Public methods
        // ================================================================

        [Description("Searches the StepinFlow user guide for how the app itself works - what a step type does, what a setting means, how to build something, why something behaves the way it does. Use this for any question about the app rather than about what the user has built. Returns nothing when the guide has no answer, which means say so rather than guess.")]
        public IReadOnlyList<AiDocumentResult> SearchAiDocuments([Description("The question, in the user's own words.")] string question)
        {
            return _aiDocumentIndexService.Search(question, _maxResults)
                .Select(x => new AiDocumentResult(x.Chunk.Citation, x.Chunk.Text))
                .ToList();
        }


        // ================================================================
        // Public types
        // ================================================================

        public record AiDocumentResult(string Source, string Text);
    }
}
