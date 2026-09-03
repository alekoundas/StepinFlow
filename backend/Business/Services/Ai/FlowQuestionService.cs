using Business.Services.Ai.AiModels;
using Business.Services.Ai.Helpers;
using Business.Services.Ai.Providers;
using Business.Services.Ai.AiDocuments;
using Business.Services.Ai.Tools;
using Core.Enums;
using Core.Models.Dtos;

using DataAccess;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;

namespace Business.Services.Ai
{
    /// <summary>
    /// Answers questions about the user's own flows by letting the model query the database.
    ///
    /// Tool calling rather than search over embedded text: "which flows use Chrome" is a WHERE
    /// clause, and a model handed the rows cannot invent a flow that is not there.
    ///
    /// The loop itself is middleware. UseFunctionInvocation does the ask, run the tool, feed the
    /// result back, ask again - so there is nothing here for a framework to do.
    /// </summary>
    public sealed class FlowQuestionService : IFlowQuestionService
    {
        /// <summary>Enough for a broad search then a detail call or two, and a stop if it loops.</summary>
        private const int _maxToolRounds = 8;

        private readonly IAiProviderService _providerService;
        private readonly IAiClientFactory _clientFactory;
        private readonly IAiModelService _modelService;
        private readonly IDbContextFactory<AppDbContext> _dbContextFactory;
        private readonly IAiDocumentIndexService _aiDocumentIndexService;
        private readonly ILogger<FlowQuestionService> _logger;

        public FlowQuestionService(
            IAiProviderService providerService,
            IAiClientFactory clientFactory,
            IAiModelService modelService,
            IDbContextFactory<AppDbContext> dbContextFactory,
            IAiDocumentIndexService aiDocumentIndexService,
            ILogger<FlowQuestionService> logger)
        {
            _providerService = providerService;
            _clientFactory = clientFactory;
            _modelService = modelService;
            _dbContextFactory = dbContextFactory;
            _aiDocumentIndexService = aiDocumentIndexService;
            _logger = logger;
        }


        // ================================================================
        // Public methods
        // ================================================================

        public async Task<AiChatAvailabilityDto> GetAvailabilityAsync(CancellationToken ct = default)
        {
            string model = await _providerService.GetModelAsync(ct);

            if (!await _providerService.IsConfiguredAsync(ct))
                return Unavailable(model, "Choose an AI provider and a model in Settings first.");

            AiProviderEnum provider = await _providerService.GetProviderAsync(ct);

            // A paid provider's models all call tools. Ollama's depend on the model, and it says so
            // itself - the same approach as the OCR languages, where only what Windows can actually
            // read is offered.
            if (provider != AiProviderEnum.OLLAMA)
                return new AiChatAvailabilityDto { IsAvailable = true, Model = model };

            if (!await _modelService.SupportsToolsAsync(ct))
                return Unavailable(model, $"{model} cannot call tools, so it would answer from nothing rather than from your flows. Qwen 2.5 and Llama 3.1 can; the bigger the model the better it chooses.");

            return new AiChatAvailabilityDto { IsAvailable = true, Model = model };
        }

        public async Task<AiChatAnswerDto> AskAsync(AiChatRequestDto request, CancellationToken ct = default)
        {
            AiChatAvailabilityDto availability = await GetAvailabilityAsync(ct);
            if (!availability.IsAvailable)
                return Failed(availability.Reason);

            using IChatClient? baseClient = await _clientFactory.CreateAsync(ct);
            if (baseClient == null)
                return Failed("AI is not set up yet. Choose a provider and a model in Settings first.");

            // THE loop.
            using IChatClient chatClient = baseClient
                .AsBuilder()
                .UseFunctionInvocation(configure: x => x.MaximumIterationsPerRequest = _maxToolRounds)
                .Build();

            List<ChatMessage> messages = [new ChatMessage(ChatRole.System, AiPromptHelper.AskAboutFlows)];
            messages.AddRange(request.Messages.Select(x => ToChatMessage(x)));

            ChatOptions options = new ChatOptions
            {
                Tools = BuildDbTools(),
                MaxOutputTokens = 1200,
            };

            try
            {
                // Actual model call.
                ChatResponse response = await chatClient.GetResponseAsync(messages, options, ct);

                return new AiChatAnswerDto
                {
                    Answer = response.Text,
                    ToolCalls = response.Messages
                        .SelectMany(x => x.Contents)
                        .OfType<FunctionCallContent>()
                        .Select(x => x.Name)
                        .ToList(),
                };
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, $"The model could not be reached. {ex.Message}");
                return Failed($"The model could not be reached. {ex.Message}");
            }
        }


        // ================================================================
        // Private methods
        // ================================================================

        private static AiChatAnswerDto Failed(string error)
        {
            return new AiChatAnswerDto { Error = error };
        }

        private static AiChatAvailabilityDto Unavailable(string model, string reason)
        {
            return new AiChatAvailabilityDto { IsAvailable = false, Model = model, Reason = reason };
        }

        private static ChatMessage ToChatMessage(AiChatMessageDto message)
        {
            ChatRole role = string.Equals(message.Role, "assistant", StringComparison.OrdinalIgnoreCase)
                ? ChatRole.Assistant
                : ChatRole.User;

            return new ChatMessage(role, message.Text);
        }


        private IList<AITool> BuildDbTools()
        {
            DbQueryTools tools = new DbQueryTools(_dbContextFactory);
            AiDocumentTools helpTools = new AiDocumentTools(_aiDocumentIndexService);

            return
            [
                AIFunctionFactory.Create(helpTools.SearchAiDocuments),
                AIFunctionFactory.Create(tools.SearchFlows),
                AIFunctionFactory.Create(tools.GetFlow),
                AIFunctionFactory.Create(tools.GetFlowSteps),
                AIFunctionFactory.Create(tools.GetFlowStepDetail),
                AIFunctionFactory.Create(tools.SearchSteps),
                AIFunctionFactory.Create(tools.GetRuns),
                AIFunctionFactory.Create(tools.GetRunSteps),
                AIFunctionFactory.Create(tools.CountStepsByType),
                AIFunctionFactory.Create(tools.CountRunOutcomes),
                AIFunctionFactory.Create(tools.GetSettings),
                AIFunctionFactory.Create(tools.GetDiscordBots),
            ];
        }
    }
}
