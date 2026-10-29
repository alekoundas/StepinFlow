using Business.Ai.Providers;
using Core.Enums;
using Core.Models.Business;

namespace Business.Tests.Fakes
{
    /// <summary>A local model, which may be sent what was on screen, unless a test says otherwise.</summary>
    public sealed class FakeAiProviderService : IAiProviderService
    {
        public bool IsConfigured { get; set; } = true;
        public string Model { get; set; } = "qwen3:8b";
        public bool CanSendScreenData { get; set; } = true;

        public Task<bool> IsConfiguredAsync(CancellationToken ct = default)
        {
            return Task.FromResult(IsConfigured);
        }

        public Task<AiSettings?> ReadAsync(CancellationToken ct = default)
        {
            AiSettings? settings = null;
            if (IsConfigured)
                settings = new AiSettings("http://localhost:11434", Model, AiCloudProviderEnum.NONE, string.Empty, string.Empty);

            return Task.FromResult(settings);
        }

        public Task<string> GetModelAsync(CancellationToken ct = default)
        {
            return Task.FromResult(Model);
        }

        public Task<bool> IsCloudModelInUseAsync(CancellationToken ct = default)
        {
            return Task.FromResult(false);
        }

        public Task<string> GetOllamaModelAsync(CancellationToken ct = default)
        {
            return Task.FromResult(Model);
        }

        public Task<string> GetOllamaUrlAsync(CancellationToken ct = default)
        {
            return Task.FromResult("http://localhost:11434");
        }

        public Task<bool> CanSendScreenDataAsync(CancellationToken ct = default)
        {
            return Task.FromResult(CanSendScreenData);
        }

        public Task<int> GetOllamaContextLengthAsync(CancellationToken ct = default)
        {
            return Task.FromResult(32768);
        }
    }
}
