using Core.Catalogs;
using Business.AppSettings;
using Core.Enums;
using Core.Models.Business;

namespace Business.Ai.Providers
{
    /// <summary>
    /// What is set up, and what it needs. Nothing here talks to a model.
    ///
    /// ReadAsync is the single definition of "configured": whatever it refuses to return is exactly
    /// what the pages decline to offer, so a check and a client build cannot disagree the way they
    /// once did - an enabled button, then "no provider is set up" to somebody who had just set one.
    ///
    /// The local model is the base. Without Ollama answering with the model chosen here nothing is
    /// on, whatever cloud model is set; a cloud model only ever answers in its place.
    /// </summary>
    public sealed class AiProviderService : IAiProviderService
    {
        /// <summary>What every AI feature says when ReadAsync has nothing to give it.</summary>
        public const string NOT_SET_UP = "AI is off until Ollama is running on this machine with a model downloaded. Choose the model in Settings.";

        private readonly IAppSettingService _appSettingService;
        private readonly IOllamaService _ollamaService;

        public AiProviderService(IAppSettingService appSettingService, IOllamaService ollamaService)
        {
            _appSettingService = appSettingService;
            _ollamaService = ollamaService;
        }


        // ================================================================
        // Public methods
        // ================================================================

        public async Task<bool> IsConfiguredAsync(CancellationToken ct = default)
        {
            return await ReadAsync(ct) != null;
        }

        // Asks Ollama every time: a model chosen once can be deleted, and Ollama can be stopped.
        public async Task<AiSettings?> ReadAsync(CancellationToken ct = default)
        {
            string ollamaModel = await GetOllamaModelAsync(ct);
            if (string.IsNullOrWhiteSpace(ollamaModel))
                return null;

            string ollamaUrl = await GetOllamaUrlAsync(ct);
            if (!await _ollamaService.HasModelAsync(ollamaUrl, ollamaModel, ct))
                return null;

            CloudModel? cloud = await ReadCloudAsync(ct);
            if (cloud == null)
                return new AiSettings(ollamaUrl, ollamaModel, AiCloudProviderEnum.NONE, string.Empty, string.Empty);

            return new AiSettings(ollamaUrl, ollamaModel, cloud.Provider, cloud.Model, cloud.ApiKey);
        }

        public async Task<string> GetModelAsync(CancellationToken ct = default)
        {
            CloudModel? cloud = await ReadCloudAsync(ct);
            if (cloud != null)
                return cloud.Model;

            return await GetOllamaModelAsync(ct);
        }

        public async Task<bool> IsCloudModelInUseAsync(CancellationToken ct = default)
        {
            return await ReadCloudAsync(ct) != null;
        }

        public Task<string> GetOllamaModelAsync(CancellationToken ct = default)
        {
            return _appSettingService.GetTextAsync(AppSettingCatalog.AiOllamaModel, ct);
        }

        public Task<string> GetOllamaUrlAsync(CancellationToken ct = default)
        {
            return _appSettingService.GetTextAsync(AppSettingCatalog.AiOllamaUrl, ct);
        }

        public async Task<bool> CanSendScreenDataAsync(CancellationToken ct = default)
        {
            if (!await IsCloudModelInUseAsync(ct))
                return true;

            string value = await _appSettingService.GetTextAsync(AppSettingCatalog.AiSendScreenContent, ct);

            return AppSettingCatalog.AiSendScreenContent.Parse(value);
        }

        public Task<int> GetOllamaContextLengthAsync(CancellationToken ct = default)
        {
            return _appSettingService.GetAsync(AppSettingCatalog.AiOllamaContextLength, ct);
        }


        // ================================================================
        // Private methods
        // ================================================================

        // A cloud model chosen without its model or its key is not in use, and the local one answers.
        private async Task<CloudModel?> ReadCloudAsync(CancellationToken ct)
        {
            string text = await _appSettingService.GetTextAsync(AppSettingCatalog.AiCloudProvider, ct);
            if (!Enum.TryParse(text, out AiCloudProviderEnum provider) || provider == AiCloudProviderEnum.NONE)
                return null;

            string model = await _appSettingService.GetTextAsync(AppSettingCatalog.AiCloudModel, ct);
            if (string.IsNullOrWhiteSpace(model))
                return null;

            string apiKey = await _appSettingService.GetTextAsync(AppSettingCatalog.AiApiKey, ct);
            if (string.IsNullOrWhiteSpace(apiKey))
                return null;

            return new CloudModel(provider, model, apiKey);
        }


        // ================================================================
        // Private types
        // ================================================================

        private sealed record CloudModel(AiCloudProviderEnum Provider, string Model, string ApiKey);
    }
}
