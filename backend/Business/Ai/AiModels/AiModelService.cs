using Business.Ai.Helpers;
using Business.Ai.Providers;
using Core.Models.Dtos;
using Microsoft.Extensions.Logging;

namespace Business.Ai.AiModels
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

        private readonly IAiProviderService _providerService;
        private readonly IOllamaService _ollamaService;
        private readonly IAiModelDownloadService _downloadService;
        private readonly ILogger<AiModelService> _logger;

        public AiModelService(
            IAiProviderService providerService,
            IOllamaService ollamaService,
            IAiModelDownloadService downloadService,
            ILogger<AiModelService> logger)
        {
            _providerService = providerService;
            _ollamaService = ollamaService;
            _downloadService = downloadService;
            _logger = logger;
        }


        // ================================================================
        // Public methods
        // ================================================================

        /// <summary>
        /// For Ollama, whatever has actually been downloaded onto this machine - anything else would
        /// offer a model that is not there. For the cloud it is a curated list.
        /// </summary>
        public async Task<AiModelsDto> GetModelsAsync(CancellationToken ct = default)
        {
            string baseUrl = await _providerService.GetOllamaUrlAsync(ct);

            IReadOnlyList<string>? names = await _ollamaService.ListModelsAsync(baseUrl, ct);
            if (names == null)
            {
                _logger.LogWarning("Could not list Ollama models at {BaseUrl}.", baseUrl);

                return new AiModelsDto
                {
                    CloudModels = _openAiModels,
                    Error = "Ollama did not answer. Check that it is installed and running, and that the address is right.",
                };
            }

            // One /api/show per model, in parallel. Serially this is a visible pause on the
            // settings page as soon as somebody has more than a couple of models pulled.
            AiModelDto[] models = await Task.WhenAll(names.Select(x => _ollamaService.DescribeAsync(baseUrl, x, ct)));

            return new AiModelsDto { Models = models, CloudModels = _openAiModels };
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
            string baseUrl = await _providerService.GetOllamaUrlAsync(ct);
            return _downloadService.Start(model, baseUrl);
        }

        /// <summary>
        /// Ollama reports what a model can do, so nothing here keeps a list that goes stale every
        /// time somebody downloads something new. A cloud model is taken from the curated list.
        /// </summary>
        public Task<bool> SupportsToolsAsync(CancellationToken ct = default)
        {
            return SupportsAsync("tools", ct);
        }

        public Task<bool> SupportsVisionAsync(CancellationToken ct = default)
        {
            return SupportsAsync("vision", ct);
        }

        // Whether the model that answers reports a capability. Nothing set up supports nothing,
        // because there is nothing to ask.
        private async Task<bool> SupportsAsync(string capability, CancellationToken ct)
        {
            string model = await _providerService.GetModelAsync(ct);
            if (string.IsNullOrWhiteSpace(model))
                return false;

            if (await _providerService.IsCloudModelInUseAsync(ct))
                return _openAiModels.FirstOrDefault(x => x.Name == model)?.Supports(capability) ?? false;

            string baseUrl = await _providerService.GetOllamaUrlAsync(ct);
            AiModelDto described = await _ollamaService.DescribeAsync(baseUrl, model, ct);

            return described.Supports(capability);
        }
    }
}
