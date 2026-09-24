using Core.Models.Business;

namespace Business.Ai.AiDocuments
{
    public interface IAiDocumentIndexService
    {
        bool IsAvailable();

        IReadOnlyList<AiDocumentSearchResult> Search(string question, int count);
    }
}
