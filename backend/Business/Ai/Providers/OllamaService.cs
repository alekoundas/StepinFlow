using System.Net.Http.Json;
using System.Text.Json;
using Business.Ai.Helpers;
using Core.Models.Dtos;
using Microsoft.Extensions.Logging;

namespace Business.Ai.Providers
{
    /// <summary>
    /// Ollama's own api: what is downloaded, and what each model can do. Asked by the settings
    /// page, and by the check that decides whether the AI features are on at all.
    /// </summary>
    public sealed class OllamaService : IOllamaService
    {
        private static readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
        };

        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<OllamaService> _logger;

        public OllamaService(IHttpClientFactory httpClientFactory, ILogger<OllamaService> logger)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }


        // ================================================================
        // Public methods
        // ================================================================

        // Not logged: Ollama not running is an ordinary state, and the status is asked on every page.
        public async Task<IReadOnlyList<string>?> ListModelsAsync(string baseUrl, CancellationToken ct = default)
        {
            try
            {
                HttpClient client = _httpClientFactory.CreateClient(nameof(OllamaService));

                using HttpResponseMessage response = await client.GetAsync(OllamaUrlHelper.ToTagsEndpoint(baseUrl), ct);
                response.EnsureSuccessStatusCode();

                await using Stream stream = await response.Content.ReadAsStreamAsync(ct);
                OllamaTagsResponse? tags = await JsonSerializer.DeserializeAsync<OllamaTagsResponse>(stream, _jsonOptions, ct);

                return tags?.Models.Select(x => x.Name).OrderBy(x => x).ToList() ?? [];
            }
            catch (Exception) when (!ct.IsCancellationRequested)
            {
                return null;
            }
        }

        public async Task<bool> HasModelAsync(string baseUrl, string model, CancellationToken ct = default)
        {
            IReadOnlyList<string>? installed = await ListModelsAsync(baseUrl, ct);
            if (installed == null)
                return false;

            string wanted = OllamaUrlHelper.NormaliseModelName(model);

            return installed.Any(x => OllamaUrlHelper.NormaliseModelName(x) == wanted);
        }

        public async Task<AiModelDto> DescribeAsync(string baseUrl, string model, CancellationToken ct = default)
        {
            try
            {
                HttpClient client = _httpClientFactory.CreateClient(nameof(OllamaService));

                using HttpResponseMessage response = await client.PostAsJsonAsync(
                    OllamaUrlHelper.ToShowEndpoint(baseUrl),
                    new { model },
                    ct);

                response.EnsureSuccessStatusCode();

                await using Stream stream = await response.Content.ReadAsStreamAsync(ct);
                OllamaShowResponse? shown = await JsonSerializer.DeserializeAsync<OllamaShowResponse>(stream, _jsonOptions, ct);

                return new AiModelDto
                {
                    Name = model,
                    Capabilities = shown?.Capabilities ?? [],
                    ContextLength = shown?.ContextLength() ?? 0,
                };
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not read the capabilities of {Model}.", model);

                return new AiModelDto { Name = model };
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
