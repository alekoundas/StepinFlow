using System.Diagnostics;
using System.Text;
using Business.Ai.AiModels;
using Business.Ai.Helpers;
using Business.Ai.Providers;
using Business.Ai.AiDocuments;
using Business.Ai.Tools;
using Core.Enums;
using Core.Models.Business;
using Core.Models.Dtos;

using DataAccess;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.AI;

namespace Business.Ai
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
        private const int MAX_TOOL_ROUNDS = 8;

        // A local model writes a hundred tokens a second, and every window is sent every broadcast.
        private const int TEXT_BATCH_MILLISECONDS = 40;

        private readonly IAiProviderService _providerService;
        private readonly IAiClientFactory _clientFactory;
        private readonly IAiModelService _modelService;
        private readonly IDbContextFactory<AppDbContext> _dbContextFactory;
        private readonly IAiDocumentIndexService _aiDocumentIndexService;
        private readonly IExecutionScreenshotReader _executionScreenshotReader;

        public FlowQuestionService(
            IAiProviderService providerService,
            IAiClientFactory clientFactory,
            IAiModelService modelService,
            IDbContextFactory<AppDbContext> dbContextFactory,
            IAiDocumentIndexService aiDocumentIndexService,
            IExecutionScreenshotReader executionScreenshotReader)
        {
            _providerService = providerService;
            _clientFactory = clientFactory;
            _modelService = modelService;
            _dbContextFactory = dbContextFactory;
            _aiDocumentIndexService = aiDocumentIndexService;
            _executionScreenshotReader = executionScreenshotReader;
        }


        // ================================================================
        // Public methods
        // ================================================================

        public async Task<AiChatAvailabilityDto> GetAvailabilityAsync(CancellationToken ct = default)
        {
            string model = await _providerService.GetModelAsync(ct);

            if (!await _providerService.IsConfiguredAsync(ct))
                return Unavailable(model, AiProviderService.NOT_SET_UP);

            // Whether a local model calls tools depends on the model, and Ollama says so itself -
            // the same approach as the OCR languages, where only what Windows can actually read is
            // offered.
            if (!await _modelService.SupportsToolsAsync(ct))
                return Unavailable(model, $"{model} cannot call tools, so it cannot read your flows to answer. Choose one that can under Settings, AI, such as qwen3.5:4b.");

            return new AiChatAvailabilityDto
            {
                IsAvailable = true,
                Model = model,
                IsCloud = await _providerService.IsCloudModelInUseAsync(ct),
                CanSendScreenData = await _providerService.CanSendScreenDataAsync(ct),
            };
        }

        public async Task AskAsync(AiChatRequestDto request, Action<AiChatEventDto> report, CancellationToken ct = default)
        {
            AiChatAvailabilityDto availability = await GetAvailabilityAsync(ct);
            if (!availability.IsAvailable)
            {
                report(Failed(request, availability.Reason));
                return;
            }

            using IChatClient? baseClient = await _clientFactory.CreateAsync(ct);
            if (baseClient == null)
            {
                report(Failed(request, AiProviderService.NOT_SET_UP));
                return;
            }

            // THE loop.
            using IChatClient chatClient = baseClient
                .AsBuilder()
                .UseFunctionInvocation(configure: x => x.MaximumIterationsPerRequest = MAX_TOOL_ROUNDS)
                .Build();

            await using AppDbContext dbContext = await _dbContextFactory.CreateDbContextAsync(ct);
            Dictionary<int, string> flowNames = await dbContext.Flows.AsNoTracking().ToDictionaryAsync(x => x.Id, x => x.Name, ct);

            string instructions = await InstructionsForAsync(request, dbContext, flowNames, ct);

            List<ChatMessage> messages = [new ChatMessage(ChatRole.System, instructions)];
            messages.AddRange(request.Messages.Select(x => ToChatMessage(x)));

            // Asked once and used by both paths, so the pictures and the rows cannot disagree
            // about what this provider is allowed to see.
            bool canSendScreenData = availability.CanSendScreenData;

            IReadOnlyList<string> attached = await AttachScreenshotsAsync(messages, request, canSendScreenData, ct);

            report(new AiChatEventDto
            {
                AnswerId = request.AnswerId,
                Type = AiChatEventTypeEnum.STARTED,
                Model = availability.Model,
                Images = attached.ToList(),
            });

            DbQueryTools dbTools = new DbQueryTools(_dbContextFactory, canSendScreenData);

            ChatOptions options = new ChatOptions
            {
                Tools = BuildDbTools(dbTools),
                MaxOutputTokens = 4000,
            };

            await StreamAnswerAsync(chatClient, messages, options, request.AnswerId, dbTools, flowNames, report, ct);
        }


        // ================================================================
        // Private methods
        // ================================================================

        private static async Task StreamAnswerAsync(
            IChatClient chatClient,
            List<ChatMessage> messages,
            ChatOptions options,
            string answerId,
            DbQueryTools dbTools,
            Dictionary<int, string> flowNames,
            Action<AiChatEventDto> report,
            CancellationToken ct)
        {
            StringBuilder answer = new StringBuilder();
            StringBuilder unsent = new StringBuilder();
            Stopwatch sinceSent = Stopwatch.StartNew();

            // Actual model call.
            await foreach (ChatResponseUpdate update in chatClient.GetStreamingResponseAsync(messages, options, ct))
            {
                foreach (AIContent content in update.Contents)
                {
                    if (content is FunctionCallContent call)
                    {
                        // What it wrote on the way to a tool was narration. The answer is what
                        // comes after the last one.
                        answer.Clear();
                        unsent.Clear();

                        report(new AiChatEventDto
                        {
                            AnswerId = answerId,
                            Type = AiChatEventTypeEnum.TOOL_STARTED,
                            ActivityId = call.CallId,
                            Activity = AiToolActivityHelper.Describe(call.Name, call.Arguments, flowNames),
                        });
                    }
                    else if (content is FunctionResultContent result)
                    {
                        report(new AiChatEventDto
                        {
                            AnswerId = answerId,
                            Type = AiChatEventTypeEnum.TOOL_FINISHED,
                            ActivityId = result.CallId,
                            Steps = dbTools.TakeSeenSteps().ToList(),
                        });
                    }
                    else if (content is TextContent text)
                    {
                        answer.Append(text.Text);
                        unsent.Append(text.Text);
                    }
                }

                if (unsent.Length > 0 && sinceSent.ElapsedMilliseconds >= TEXT_BATCH_MILLISECONDS)
                {
                    report(new AiChatEventDto { AnswerId = answerId, Type = AiChatEventTypeEnum.TEXT, Text = unsent.ToString() });
                    unsent.Clear();
                    sinceSent.Restart();
                }
            }

            // Ollama's stream just ends when it is cancelled, rather than throwing.
            ct.ThrowIfCancellationRequested();

            string written = answer.ToString().Trim();
            if (written.Length == 0)
            {
                report(Failed(answerId, "The model finished without writing an answer."));
                return;
            }

            report(new AiChatEventDto { AnswerId = answerId, Type = AiChatEventTypeEnum.DONE, Text = written });
        }

        private static AiChatEventDto Failed(AiChatRequestDto request, string error)
        {
            return Failed(request.AnswerId, error);
        }

        private static AiChatEventDto Failed(string answerId, string error)
        {
            return new AiChatEventDto { AnswerId = answerId, Type = AiChatEventTypeEnum.FAILED, Error = error };
        }

        private static AiChatAvailabilityDto Unavailable(string model, string reason)
        {
            return new AiChatAvailabilityDto { IsAvailable = false, Model = model, Reason = reason };
        }

        private static async Task<string> InstructionsForAsync(AiChatRequestDto request, AppDbContext dbContext, Dictionary<int, string> flowNames, CancellationToken ct)
        {
            string lookingAt = string.Empty;

            if (request.ExecutionId != null)
            {
                int flowId = await dbContext.Executions
                    .AsNoTracking()
                    .Where(x => x.Id == request.ExecutionId)
                    .Select(x => x.FlowId)
                    .FirstOrDefaultAsync(ct);

                if (flowNames.TryGetValue(flowId, out string? flowName))
                    lookingAt = AiPromptHelper.LookingAtExecution(request.ExecutionId.Value, flowId, flowName, request.FlowStepId);
            }
            else if (request.FlowId != null && flowNames.TryGetValue(request.FlowId.Value, out string? flowName))
            {
                lookingAt = AiPromptHelper.LookingAtFlow(request.FlowId.Value, flowName);
            }

            if (lookingAt.Length == 0)
                return AiPromptHelper.AskAboutFlows;

            return $"{AiPromptHelper.AskAboutFlows}\n\n{lookingAt}";
        }

        // Pictures ride on the newest question only, never on the ones behind it.
        //
        // UseFunctionInvocation replays the whole conversation on every round of the tool loop, up
        // to MAX_TOOL_ROUNDS, so an image left in the history is re-read that many times per
        // question. Ollama's cpu backend keeps no cache between requests, so nothing absorbs that.
        private async Task<IReadOnlyList<string>> AttachScreenshotsAsync(List<ChatMessage> messages, AiChatRequestDto request, bool canSendScreenData, CancellationToken ct)
        {
            if (request.ExecutionId == null || !request.SendScreenshots)
                return [];

            // A screenshot cannot be redacted - a font the OCR does not know, a language it does
            // not have, text drawn into an image. So it is all or nothing, and nothing is the
            // default for a cloud provider.
            if (!canSendScreenData)
                return [];

            ChatMessage? newest = messages.LastOrDefault(x => x.Role == ChatRole.User);
            if (newest == null)
                return [];

            IReadOnlyList<AiImage> images = await _executionScreenshotReader.GetForExecutionAsync(request.ExecutionId.Value, ct);
            if (images.Count == 0)
                return [];

            // Each one says what it is before it arrives. Unlabelled, they are just pictures, and a
            // model describes pictures instead of comparing them.
            foreach (AiImage image in images)
            {
                newest.Contents.Add(new TextContent(image.Label));
                newest.Contents.Add(new DataContent(image.Bytes, image.MediaType));
            }

            return images.Select(x => x.Name).ToList();
        }

        private static ChatMessage ToChatMessage(AiChatMessageDto message)
        {
            ChatRole role = string.Equals(message.Role, "assistant", StringComparison.OrdinalIgnoreCase)
                ? ChatRole.Assistant
                : ChatRole.User;

            return new ChatMessage(role, message.Text);
        }


        private IList<AITool> BuildDbTools(DbQueryTools tools)
        {
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
                AIFunctionFactory.Create(tools.GetFlowChecks),
                AIFunctionFactory.Create(tools.CountRunOutcomes),
                AIFunctionFactory.Create(tools.GetSettings),
                AIFunctionFactory.Create(tools.GetDiscordBots),
            ];
        }
    }
}
