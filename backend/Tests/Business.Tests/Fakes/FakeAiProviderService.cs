using Business.Ai.Providers;
using Core.Enums;
using Core.Models.Business;

namespace Business.Tests.Fakes
{
    /// <summary>A local model, which may be sent what was on screen, unless a test says otherwise.</summary>
    public sealed class FakeAiProviderService : IAiProviderService
    {
        public AiProviderEnum Provider { get; set; } = AiProviderEnum.OLLAMA;
        public string Model { get; set; } = "qwen3:8b";
        public bool CanSendScreenData { get; set; } = true;

        public Task<AiProviderEnum> GetProviderAsync(CancellationToken ct = default)
        {
            return Task.FromResult(Provider);
        }

        public Task<bool> IsConfiguredAsync(CancellationToken ct = default)
        {
            return Task.FromResult(Provider != AiProviderEnum.NONE);
        }

        public Task<AiSettings?> ReadAsync(CancellationToken ct = default)
        {
            AiSettings? settings = null;
            if (Provider != AiProviderEnum.NONE)
                settings = new AiSettings(Provider, Model, "ollama", "http://localhost:11434");

            return Task.FromResult(settings);
        }

        public Task<string> GetModelAsync(CancellationToken ct = default)
        {
            return Task.FromResult(Model);
        }

        public Task<string> GetOllamaUrlAsync(CancellationToken ct = default)
        {
            return Task.FromResult("http://localhost:11434");
        }

        public Task<bool> IsScreenContentAllowedAsync(CancellationToken ct = default)
        {
            return Task.FromResult(CanSendScreenData);
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
