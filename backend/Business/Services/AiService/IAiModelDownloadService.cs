using Core.Models.Dtos;

namespace Business.Services.AiService
{
    public interface IAiModelDownloadService
    {
        /// <summary>
        /// How the current or last download went. Held here rather than in the page, because a
        /// download outlives whatever was on screen when it started.
        /// </summary>
        AiModelPullEventDto? Current { get; }

        /// <summary>Starts one and returns. False when one is already running.</summary>
        bool Start(string model, string baseUrl);

        /// <summary>Forgets a finished one, so the page stops showing it.</summary>
        void Clear();
    }
}
