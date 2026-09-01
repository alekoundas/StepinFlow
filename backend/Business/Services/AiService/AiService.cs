using System.Text.Json;

using Business.Helpers;
using Business.Services.AppSettingService;
using Core.Enums;
using Core.Helpers;
using Core.Models.Dtos;

using DataAccess;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;

namespace Business.Services.AiService
{
    /// <summary>
    /// The AI features, such as they are: read a run, say what went wrong.
    ///
    /// Nothing in here knows whether the model is on this machine or on the other side of the
    /// world. That is the factory's business, and the only difference it makes is how long the
    /// call takes.
    /// </summary>
    public sealed class AiService : IAiService
    {
        /// <summary>The ones worth offering. Asking OpenAI for its list returns hundreds.</summary>
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

        private readonly IDbContextFactory<AppDbContext> _dbContextFactory;
        private readonly IChatClientFactory _chatClientFactory;
        private readonly IAiModelDownloadService _downloadService;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IAppSettingService _appSettingService;
        private readonly ILogger<AiService> _logger;

        public AiService(
            IDbContextFactory<AppDbContext> dbContextFactory,
            IChatClientFactory chatClientFactory,
            IAiModelDownloadService downloadService,
            IHttpClientFactory httpClientFactory,
            IAppSettingService appSettingService,
            ILogger<AiService> logger)
        {
            _dbContextFactory = dbContextFactory;
            _chatClientFactory = chatClientFactory;
            _downloadService = downloadService;
            _httpClientFactory = httpClientFactory;
            _appSettingService = appSettingService;
            _logger = logger;
        }


        // ================================================================
        // Public methods
        // ================================================================

        public Task<bool> IsConfiguredAsync(CancellationToken ct = default)
        {
            return _chatClientFactory.IsConfiguredAsync(ct);
        }

        /// <summary>
        /// For Ollama, whatever has actually been pulled onto this machine - anything else would
        /// offer a model that is not there. For a paid provider it is a curated list, because
        /// asking the api returns every embedding and speech model alongside the useful ones.
        /// </summary>
        public async Task<AiModelsDto> GetModelsAsync(CancellationToken ct = default)
        {
            AiProviderEnum provider = await _chatClientFactory.GetProviderAsync(ct);

            if (provider == AiProviderEnum.OPENAI)
                return new AiModelsDto { Models = _openAiModels };

            if (provider != AiProviderEnum.OLLAMA)
                return new AiModelsDto();

            string baseUrl = await _appSettingService.GetTextAsync(AppSettingCatalog.AiOllamaUrl, ct);

            try
            {
                HttpClient client = _httpClientFactory.CreateClient(nameof(AiService));

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

            return AiModelCatalog.Suggestions
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
        /// Hands the download to the service that owns it. That one is a singleton, because a pull
        /// runs for minutes and must outlive both this scope and whatever page asked for it.
        /// </summary>
        public async Task<bool> StartModelPullAsync(string model, CancellationToken ct = default)
        {
            AiProviderEnum provider = await _chatClientFactory.GetProviderAsync(ct);
            if (provider != AiProviderEnum.OLLAMA)
                return false;

            string baseUrl = await _appSettingService.GetTextAsync(AppSettingCatalog.AiOllamaUrl, ct);
            return _downloadService.Start(model, baseUrl);
        }

        public async Task<AiAnswerDto> ExplainExecutionAsync(int executionId, CancellationToken ct = default)
        {
            // Built per call from the settings, so it is ours to dispose. Explain is a button
            // somebody presses repeatedly while reading a run.
            using IChatClient? chatClient = await _chatClientFactory.CreateAsync(ct);
            if (chatClient == null)
                return Failed("AI is not set up yet. Choose a provider and a model in Settings first.");

            ExecutionDto? execution = await LoadAsync(executionId, ct);
            if (execution == null)
                return Failed("That run no longer exists.");

            bool includeScreenValues = await GetIncludeScreenValuesAsync(ct);
            string prompt = ExecutionRunFormatter.Format(execution, includeScreenValues);

            List<ChatMessage> messages =
            [
                new ChatMessage(ChatRole.System, AiPromptHelper.ExplainExecution),
                new ChatMessage(ChatRole.User, prompt),
            ];

            try
            {
                ChatResponse response = await chatClient.GetResponseAsync(messages, cancellationToken: ct);

                return new AiAnswerDto
                {
                    Answer = response.Text,
                    Prompt = prompt,
                };
            }
            catch (Exception ex)
            {
                // A model that will not answer is not worth an unhandled exception, and the message
                // is the only useful thing here - a wrong key and an unreachable Ollama look alike.
                _logger.LogWarning(ex, "Could not explain execution {ExecutionId}.", executionId);

                AiAnswerDto failed = Failed($"The model could not be reached. {ex.Message}");
                failed.Prompt = prompt;

                return failed;
            }
        }


        // ================================================================
        // Private methods
        // ================================================================

        private static AiAnswerDto Failed(string error)
        {
            return new AiAnswerDto
            {
                Error = error,
            };
        }

        /// <summary>
        /// Whether the model may see text a Read Text step found. That is whatever was on the
        /// screen - an account number, a password an OCR step happened to catch - so it goes out
        /// only to a model running on this machine, and the provider is the whole rule. There is
        /// no setting to disagree with.
        /// </summary>
        private async Task<bool> GetIncludeScreenValuesAsync(CancellationToken ct)
        {
            AiProviderEnum provider = await _chatClientFactory.GetProviderAsync(ct);
            return provider == AiProviderEnum.OLLAMA;
        }

        /// <summary>
        /// The run and its steps, projected the same way the execution page reads them.
        /// </summary>
        private async Task<ExecutionDto?> LoadAsync(int executionId, CancellationToken ct)
        {
            await using AppDbContext dbContext = await _dbContextFactory.CreateDbContextAsync(ct);

            ExecutionDto? execution = await dbContext.Executions
                .AsNoTracking()
                .Where(x => x.Id == executionId)
                .Select(x => new ExecutionDto
                {
                    Id = x.Id,
                    Status = x.Status,
                    StepCount = x.StepCount,
                    ErrorFlowStepId = x.ErrorFlowStepId,
                    ErrorMessage = x.ErrorMessage,
                    FlowId = x.FlowId,
                })
                .FirstOrDefaultAsync(ct);

            if (execution == null)
                return null;

            execution.ExecutionSteps = await dbContext.ExecutionSteps
                .AsNoTracking()
                .Where(x => x.ExecutionId == executionId)
                .OrderBy(x => x.Sequence)
                .Select(x => new ExecutionStepDto
                {
                    Sequence = x.Sequence,
                    ParentSequence = x.ParentSequence,
                    Depth = x.Depth,
                    LoopPass = x.LoopPass,
                    Name = x.Name,
                    FlowStepType = x.FlowStepType,
                    Outcome = x.Outcome,
                    DurationMilliseconds = x.DurationMilliseconds,
                    MatchIndex = x.MatchIndex,
                    MatchCount = x.MatchCount,
                    Value = x.Value,
                    Message = x.Message,
                    ExitCode = x.ExitCode,
                    Error = x.Error,
                    FlowStepId = x.FlowStepId,
                })
                .ToListAsync(ct);

            return execution;
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

    }
}
