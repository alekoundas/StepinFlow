using Core.Models.Dtos;

namespace Business.Ai.AiModels
{
    public interface IAiModelDownloadService
    {
        AiModelDownloadEventDto? Current { get; }
        bool Start(string model, string baseUrl);
        void Clear();
    }
}
