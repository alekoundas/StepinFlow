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
        // Hand kept, because OpenAI has no endpoint that answers this the way Ollama's /api/show
        // does. Every model here reads images and calls tools; the numbers are their published
        // context windows.
        private static readonly AiModelDto[] _openAiModels =
        [
            new AiModelDto { Name = "gpt-4o-mini", Capabilities = ["completion", "tools", "vision"], ContextLength = 128_000 },
            new AiModelDto { Name = "gpt-4o", Capabilities = ["completion", "tools", "vision"], ContextLength = 128_000 },
            new AiModelDto { Name = "gpt-4.1-mini", Capabilities = ["completion", "tools", "vision"], ContextLength = 1_000_000 },
            new AiModelDto { Name = "gpt-4.1", Capabilities = ["completion", "tools", "vision"], ContextLength = 1_000_000 },
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

                List<string> names = tags?.Models.Select(x => x.Name).OrderBy(x => x).ToList() ?? [];

                // One /api/show per model, in parallel. Serially this is a visible pause on the
                // settings page as soon as somebody has more than a couple of models pulled.
                AiModelDto[] models = await Task.WhenAll(names.Select(x => DescribeAsync(baseUrl, x, ct)));

                return new AiModelsDto { Models = models };
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

        // What one model can do, straight from Ollama. A model that will not answer still comes
        // back - it is in the list either way, just without badges.
        private async Task<AiModelDto> DescribeAsync(string baseUrl, string name, CancellationToken ct)
        {
            try
            {
                HttpClient client = _httpClientFactory.CreateClient(nameof(AiModelService));

                using HttpResponseMessage response = await client.PostAsJsonAsync(
                    OllamaUrlHelper.ToShowEndpoint(baseUrl),
                    new { model = name },
                    ct);

                response.EnsureSuccessStatusCode();

                await using Stream stream = await response.Content.ReadAsStreamAsync(ct);
                OllamaShowResponse? shown = await JsonSerializer.DeserializeAsync<OllamaShowResponse>(stream, _jsonOptions, ct);

                return new AiModelDto
                {
                    Name = name,
                    Capabilities = shown?.Capabilities ?? [],
                    ContextLength = shown?.ContextLength() ?? 0,
                };
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not read the capabilities of {Model}.", name);

                return new AiModelDto { Name = name };
            }
        }

        public async Task<IReadOnlyList<AiModelSuggestionDto>> GetModelSuggestionsAsync(CancellationToken ct = default)
        {
            AiModelsDto installed = await GetModelsAsync(ct);

            // Compared whole, tag included. The tag is the model - qwen2.5:3b and qwen2.5:7b are
            // different downloads - so matching on the name alone would mark both as installed
            // when only one of them is.
            HashSet<string> names = installed.Models
                .Select(x => x.Name)
                .Select(OllamaUrlHelper.NormaliseModelName)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            return AvailableAiModelsHelper.Suggestions
                .Select(x => new AiModelSuggestionDto
                {
                    Name = x.Name,
                    Size = x.Size,
                    Description = x.Description,
                    Capabilities = x.Capabilities,
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
        public Task<bool> SupportsToolsAsync(CancellationToken ct = default)
        {
            return SupportsAsync("tools", ct);
        }

        public Task<bool> SupportsVisionAsync(CancellationToken ct = default)
        {
            return SupportsAsync("vision", ct);
        }

        // Whether the model in settings reports a capability. Only Ollama answers this per model:
        // a cloud provider is taken at its word from the curated list, and NONE supports nothing
        // because there is nothing to ask.
        private async Task<bool> SupportsAsync(string capability, CancellationToken ct)
        {
            AiProviderEnum provider = await _providerService.GetProviderAsync(ct);
            if (provider == AiProviderEnum.NONE)
                return false;

            string model = await _providerService.GetModelAsync(ct);
            if (string.IsNullOrWhiteSpace(model))
                return false;

            if (provider != AiProviderEnum.OLLAMA)
                return _openAiModels.FirstOrDefault(x => x.Name == model)?.Supports(capability) ?? false;

            string baseUrl = await _providerService.GetOllamaUrlAsync(ct);
            AiModelDto described = await DescribeAsync(baseUrl, model, ct);

            return described.Supports(capability);
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

            // Everything the gguf declares. The context length is under a key named after the
            // architecture - qwen2.context_length, llama.context_length - so it is found by its
            // suffix rather than by guessing the family.
            public Dictionary<string, JsonElement> ModelInfo { get; set; } = new Dictionary<string, JsonElement>();

            public int ContextLength()
            {
                foreach (KeyValuePair<string, JsonElement> entry in ModelInfo)
                {
                    if (!entry.Key.EndsWith(".context_length", StringComparison.OrdinalIgnoreCase))
                        continue;

                    if (entry.Value.ValueKind == JsonValueKind.Number && entry.Value.TryGetInt32(out int length))
                        return length;
                }

                return 0;
            }
        }
    }
}
