using Business.Services.Ai.Helpers;
using Business.Services.Ai.Providers;
using Core.Enums;
using Core.Models.Dtos;

using DataAccess;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;

namespace Business.Services.Ai
{
    /// <summary>
    /// Feature one: read a finished run and say what went wrong.
    ///
    /// Stuffed context, no tools. The whole run goes in one message, trimmed to what explains the
    /// failure, and the quality of the answer comes from that formatting far more than the prompt.
    /// </summary>
    public sealed class ExecutionRunExplainService : IExecutionRunExplainService
    {
        private readonly IDbContextFactory<AppDbContext> _dbContextFactory;
        private readonly IAiProviderService _providerService;
        private readonly IAiClientFactory _clientFactory;
        private readonly ILogger<ExecutionRunExplainService> _logger;

        public ExecutionRunExplainService(
            IDbContextFactory<AppDbContext> dbContextFactory,
            IAiProviderService providerService,
            IAiClientFactory clientFactory,
            ILogger<ExecutionRunExplainService> logger)
        {
            _dbContextFactory = dbContextFactory;
            _providerService = providerService;
            _clientFactory = clientFactory;
            _logger = logger;
        }


        // ================================================================
        // Public methods
        // ================================================================

        public async Task<AiAnswerDto> ExplainExecutionAsync(int executionId, CancellationToken ct = default)
        {
            // Built per call from the settings, so it is ours to dispose. Explain is a button
            // somebody presses repeatedly while reading a run.
            using IChatClient? chatClient = await _clientFactory.CreateAsync(ct);
            if (chatClient == null)
                return Failed("AI is not set up yet. Choose a provider and a model in Settings first.");

            ExecutionDto? execution = await LoadExecutionAndStepsAsync(executionId, ct);
            if (execution == null)
                return Failed("That run no longer exists.");

            bool includeScreenValues = await GetIncludeScreenValuesAsync(ct);
            string prompt = AiPromptHelper.FormatExecution(execution, includeScreenValues);

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
        /// Text a Read Text step found is whatever was on the screen, so it goes out only to a model
        /// running on this machine. The provider is the whole rule; there is no setting to disagree.
        /// </summary>
        private async Task<bool> GetIncludeScreenValuesAsync(CancellationToken ct)
        {
            AiProviderEnum provider = await _providerService.GetProviderAsync(ct);
            return provider == AiProviderEnum.OLLAMA;
        }

        private async Task<ExecutionDto?> LoadExecutionAndStepsAsync(int executionId, CancellationToken ct)
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
                    BestScore = x.BestScore,
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
    }
}
