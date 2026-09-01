using System.Net.Http.Json;
using System.Text.Json;
using Business.Services.Ai.Helpers;
using Business.Services.Ai.Providers;
using Core.Enums;
using Core.Models.Dtos;
using Microsoft.Extensions.Logging;

namespace Business.Services.Ai.AiModels
{
    /// <summary>
    /// Which models can be picked, what they can do, and getting one onto the machine.
    /// Nothing here asks a model anything - that is each feature's own business.
    /// </summary>
    public sealed class AiModelService : IAiModelService
    {
        // The ones worth offering. Asking OpenAI returns hundreds.
        private static readonly string[] _openAiModels =
        [
            "gpt-4o-mini",
            "gpt-4o",
            "gpt-4.1-mini",
            "gpt-4.1",
        ];

        private static readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
        };

        private readonly IAiProviderService _providerService;
        private readonly IAiModelDownloadService _downloadService;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<AiModelService> _logger;

        public AiModelService(
            IAiProviderService providerService,
            IAiModelDownloadService downloadService,
            IHttpClientFactory httpClientFactory,
            ILogger<AiModelService> logger)
        {
            _providerService = providerService;
            _downloadService = downloadService;
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }


        // ================================================================
        // Public methods
        // ================================================================

        /// <summary>
        /// For Ollama, whatever has actually been downloaded onto this machine - anything else would
        /// offer a model that is not there. For a paid provider it is a curated list.
        /// </summary>
        public async Task<AiModelsDto> GetModelsAsync(CancellationToken ct = default)
        {
            AiProviderEnum provider = await _providerService.GetProviderAsync(ct);

            if (provider == AiProviderEnum.OPENAI)
                return new AiModelsDto { Models = _openAiModels };

            if (provider != AiProviderEnum.OLLAMA)
                return new AiModelsDto();

            string baseUrl = await _providerService.GetOllamaUrlAsync(ct);

            try
            {
                HttpClient client = _httpClientFactory.CreateClient(nameof(AiModelService));

                using HttpResponseMessage response = await client.GetAsync(OllamaUrlHelper.ToTagsEndpoint(baseUrl), ct);
                response.EnsureSuccessStatusCode();

                await using Stream stream = await response.Content.ReadAsStreamAsync(ct);
                OllamaTagsResponse? tags = await JsonSerializer.DeserializeAsync<OllamaTagsResponse>(stream, _jsonOptions, ct);

                return new AiModelsDto
                {
                    Models = tags?.Models.Select(x => x.Name).OrderBy(x => x).ToList() ?? [],
                };
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not list Ollama models at {BaseUrl}.", baseUrl);

                return new AiModelsDto
                {
                    Error = "Ollama did not answer. Check that it is running, and that the address is right.",
                };
            }
        }

        public async Task<IReadOnlyList<AiModelSuggestionDto>> GetModelSuggestionsAsync(CancellationToken ct = default)
        {
            AiModelsDto installed = await GetModelsAsync(ct);

            // Compared whole, tag included. The tag is the model - qwen2.5:3b and qwen2.5:7b are
            // different downloads - so matching on the name alone would mark both as installed
            // when only one of them is.
            HashSet<string> names = installed.Models
                .Select(OllamaUrlHelper.NormaliseModelName)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            return AvailableAiModelsHelper.Suggestions
                .Select(x => new AiModelSuggestionDto
                {
                    Name = x.Name,
                    Size = x.Size,
                    Description = x.Description,
                    IsInstalled = names.Contains(OllamaUrlHelper.NormaliseModelName(x.Name)),
                })
                .ToList();
        }

        /// <summary>
        /// Hands the download to the service that owns it. That one is a singleton, because a download
        /// runs for minutes and must outlive both this scope and whatever page asked for it.
        /// </summary>
        public async Task<bool> StartModelDownloadAsync(string model, CancellationToken ct = default)
        {
            AiProviderEnum provider = await _providerService.GetProviderAsync(ct);
            if (provider != AiProviderEnum.OLLAMA)
                return false;

            string baseUrl = await _providerService.GetOllamaUrlAsync(ct);
            return _downloadService.Start(model, baseUrl);
        }

        /// <summary>
        /// Ollama reports what a model can do, so nothing here keeps a list that goes stale every
        /// time somebody downloads something new. A paid provider's models all call tools.
        /// </summary>
        public async Task<bool> SupportsToolsAsync(CancellationToken ct = default)
        {
            AiProviderEnum provider = await _providerService.GetProviderAsync(ct);
            if (provider != AiProviderEnum.OLLAMA)
                return provider != AiProviderEnum.NONE;

            string model = await _providerService.GetModelAsync(ct);
            if (string.IsNullOrWhiteSpace(model))
                return false;

            string baseUrl = await _providerService.GetOllamaUrlAsync(ct);

            try
            {
                HttpClient client = _httpClientFactory.CreateClient(nameof(AiModelService));

                using HttpResponseMessage response = await client.PostAsJsonAsync(
                    OllamaUrlHelper.ToShowEndpoint(baseUrl),
                    new { model = model },
                    ct);

                response.EnsureSuccessStatusCode();

                await using Stream stream = await response.Content.ReadAsStreamAsync(ct);
                OllamaShowResponse? shown = await JsonSerializer.DeserializeAsync<OllamaShowResponse>(stream, _jsonOptions, ct);

                return shown?.Capabilities.Any(x => string.Equals(x, "tools", StringComparison.OrdinalIgnoreCase)) == true;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not ask Ollama what {Model} can do.", model);
                return false;
            }
        }


        // ================================================================
        // Private types
        // ================================================================

        private sealed class OllamaTagsResponse
        {
            public List<OllamaTag> Models { get; set; } = [];
        }

        private sealed class OllamaTag
        {
            public string Name { get; set; } = string.Empty;
        }

        private sealed class OllamaShowResponse
        {
            public List<string> Capabilities { get; set; } = [];
        }
    }
}
