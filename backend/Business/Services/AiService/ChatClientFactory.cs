using System.ClientModel;

using Business.Helpers;
using Business.Services.AppSettingService;
using Core.Enums;
using Core.Helpers;

using Microsoft.Extensions.AI;
using OpenAI;

namespace Business.Services.AiService
{
    /// <summary>
    /// Turns the settings into a client.
    ///
    /// Local and cloud are the same client with a different address: Ollama serves an OpenAI
    /// compatible api, so pointing at localhost is the whole of "run it on this machine". Nothing
    /// above this class ever learns which one it got.
    /// </summary>
    public sealed class ChatClientFactory : IChatClientFactory
    {
        private readonly IAppSettingService _appSettingService;

        public ChatClientFactory(IAppSettingService appSettingService)
        {
            _appSettingService = appSettingService;
        }


        // ================================================================
        // Public methods
        // ================================================================

        public async Task<AiProviderEnum> GetProviderAsync(CancellationToken ct = default)
        {
            string text = await _appSettingService.GetTextAsync(AppSettingCatalog.AiProvider, ct);

            if (Enum.TryParse(text, out AiProviderEnum provider))
                return provider;

            return AiProviderEnum.NONE;
        }

        public async Task<bool> IsConfiguredAsync(CancellationToken ct = default)
        {
            return await ReadAsync(ct) != null;
        }

        public async Task<IChatClient?> CreateAsync(CancellationToken ct = default)
        {
            AiSettings? settings = await ReadAsync(ct);
            if (settings == null)
                return null;

            OpenAIClientOptions options = new OpenAIClientOptions();

            if (settings.Provider == AiProviderEnum.OLLAMA)
                options.Endpoint = new Uri(OllamaUrlHelper.ToOpenAiEndpoint(settings.OllamaUrl));

            OpenAIClient client = new OpenAIClient(new ApiKeyCredential(settings.ApiKey), options);
            return client.GetChatClient(settings.Model).AsIChatClient();
        }


        // ================================================================
        // Private methods
        // ================================================================

        private async Task<AiSettings?> ReadAsync(CancellationToken ct)
        {
            AiProviderEnum provider = await GetProviderAsync(ct);
            if (provider == AiProviderEnum.NONE)
                return null;

            string model = await _appSettingService.GetTextAsync(AppSettingCatalog.AiModel, ct);
            if (string.IsNullOrWhiteSpace(model))
                return null;

            if (provider == AiProviderEnum.OLLAMA)
            {
                string url = await _appSettingService.GetTextAsync(AppSettingCatalog.AiOllamaUrl, ct);

                // Ollama wants the header present and does not look at it.
                return new AiSettings(provider, model, "ollama", url);
            }

            string apiKey = await _appSettingService.GetTextAsync(AppSettingCatalog.AiApiKey, ct);
            if (string.IsNullOrWhiteSpace(apiKey))
                return null;

            return new AiSettings(provider, model, apiKey, string.Empty);
        }


        // ================================================================
        // Private types
        // ================================================================

        private sealed record AiSettings(AiProviderEnum Provider, string Model, string ApiKey, string OllamaUrl);
    }
}
