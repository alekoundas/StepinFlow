using Core.Models.Business;

namespace Business.Services.Ai.AiDocuments
{
    public interface IAiDocumentIndexService
    {
        bool IsAvailable();

        IReadOnlyList<AiDocumentSearchResult> Search(string question, int count);
    }
}
