using Business.Services.AppSettingService;
using Core.Enums;
using Core.Helpers;
using Core.Models.Business;

namespace Business.Services.Ai.Providers
{
    /// <summary>
    /// What is set up, and what it needs. Nothing here talks to a model.
    ///
    /// ReadAsync is the single definition of "configured": whatever it refuses to return is exactly
    /// what the pages decline to offer, so a check and a client build cannot disagree the way they
    /// once did - an enabled button, then "no provider is set up" to somebody who had just set one.
    /// </summary>
    public sealed class AiProviderService : IAiProviderService
    {
        private readonly IAppSettingService _appSettingService;

        public AiProviderService(IAppSettingService appSettingService)
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

        public async Task<AiSettings?> ReadAsync(CancellationToken ct = default)
        {
            AiProviderEnum provider = await GetProviderAsync(ct);
            if (provider == AiProviderEnum.NONE)
                return null;

            string model = await GetModelAsync(ct);
            if (string.IsNullOrWhiteSpace(model))
                return null;

            if (provider == AiProviderEnum.OLLAMA)
            {
                string url = await GetOllamaUrlAsync(ct);

                // Ollama wants the header present and does not look at it.
                return new AiSettings(provider, model, "ollama", url);
            }

            string apiKey = await _appSettingService.GetTextAsync(AppSettingCatalog.AiApiKey, ct);
            if (string.IsNullOrWhiteSpace(apiKey))
                return null;

            return new AiSettings(provider, model, apiKey, string.Empty);
        }

        public Task<string> GetModelAsync(CancellationToken ct = default)
        {
            return _appSettingService.GetTextAsync(AppSettingCatalog.AiModel, ct);
        }

        public async Task<bool> IsScreenContentAllowedAsync(CancellationToken ct = default)
        {
            string value = await _appSettingService.GetTextAsync(AppSettingCatalog.AiSendScreenContent, ct);

            return AppSettingCatalog.AiSendScreenContent.Parse(value);
        }

        public Task<string> GetOllamaUrlAsync(CancellationToken ct = default)
        {
            return _appSettingService.GetTextAsync(AppSettingCatalog.AiOllamaUrl, ct);
        }
    }
}
