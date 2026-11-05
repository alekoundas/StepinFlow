using Business.Ai;
using Business.Ai.AiDocuments;
using Business.Ai.AiModels;
using Business.Ai.Tools;
using Business.Tests.Fakes;
using Core.Enums;
using Core.Models.Business;
using Core.Models.Database;
using Core.Models.Dtos;
using DataAccess;
using Microsoft.Extensions.AI;
using static Business.Tests.TestToken;

namespace Business.Tests.Ai
{
    /// <summary>An answer as it comes: what it looked at in plain words, then only the words that answer.</summary>
    public sealed class FlowQuestionServiceTests : IDisposable
    {
        private const string ANSWER_ID = "answer-1";

        private readonly TestDatabase _database = new TestDatabase();
        private readonly FakeAiProviderService _provider = new FakeAiProviderService();
        private readonly FakeAiClientFactory _clients = new FakeAiClientFactory();
        private readonly FakeModelService _models = new FakeModelService();
        private readonly FakeScreenshotReader _screenshots = new FakeScreenshotReader();
        private readonly List<AiChatEventDto> _events = new List<AiChatEventDto>();
        private readonly FlowQuestionService _service;

        public FlowQuestionServiceTests()
        {
            _service = new FlowQuestionService(_provider, _clients, _models, _database, new NoDocuments(), _screenshots);
        }

        public void Dispose()
        {
            _database.Dispose();
        }

        private FakeStreamingChatClient UseModel(params IReadOnlyList<AIContent>[] rounds)
        {
            FakeStreamingChatClient model = new FakeStreamingChatClient(rounds);
            _clients.Client = model;

            return model;
        }

        private Task AskAsync(int? executionId = null, bool sendScreenshots = false, int? flowId = null, CancellationToken? ct = null)
        {
            AiChatRequestDto request = new AiChatRequestDto
            {
                AnswerId = ANSWER_ID,
                Messages = [new AiChatMessageDto { Role = "user", Text = "Why did it fail?" }],
                ExecutionId = executionId,
                FlowId = flowId,
                SendScreenshots = sendScreenshots,
            };

            return _service.AskAsync(request, x => _events.Add(x), ct ?? Ct);
        }

        private List<AiChatEventDto> EventsOf(AiChatEventTypeEnum type)
        {
            return _events.Where(x => x.Type == type).ToList();
        }

        private string WrittenText()
        {
            return string.Concat(EventsOf(AiChatEventTypeEnum.TEXT).Select(x => x.Text));
        }

        private int AddFlowWithExecution(string flowName, string stepName)
        {
            using AppDbContext db = _database.CreateDbContext();

            Flow flow = new Flow { Name = flowName, PublicId = Guid.NewGuid() };
            db.Flows.Add(flow);
            db.SaveChanges();

            FlowStep step = new FlowStep { Name = stepName, FlowStepType = FlowStepTypeEnum.WAIT, RootId = flow.Id, FlowId = flow.Id };
            db.FlowSteps.Add(step);
            db.SaveChanges();

            Execution execution = new Execution { FlowId = flow.Id, Status = ExecutionStatusEnum.FAILED };
            db.Executions.Add(execution);
            db.SaveChanges();

            db.ExecutionSteps.Add(new ExecutionStep { ExecutionId = execution.Id, Sequence = 1, Name = stepName, FlowStepId = step.Id, FlowStepType = FlowStepTypeEnum.WAIT, Outcome = StepOutcomeEnum.FAILURE });
            db.SaveChanges();

            return execution.Id;
        }

        private static FunctionCallContent ToolCall(string callId, string name, params (string Name, object? Value)[] arguments)
        {
            return new FunctionCallContent(callId, name, arguments.ToDictionary(x => x.Name, x => x.Value));
        }

        // ================================================================
        // The answer
        // ================================================================

        [Fact]
        public async Task An_answer_is_started_written_and_ended()
        {
            UseModel([new TextContent("The check "), new TextContent("read nothing.")]);

            await AskAsync();

            _events.First().Type.ShouldBe(AiChatEventTypeEnum.STARTED);
            _events.First().Model.ShouldBe(_provider.Model);
            _events.ShouldAllBe(x => x.AnswerId == ANSWER_ID);
            EventsOf(AiChatEventTypeEnum.DONE).Single().Text.ShouldBe("The check read nothing.");
        }

        [Fact]
        public async Task What_it_wrote_on_the_way_to_a_tool_is_not_the_answer()
        {
            UseModel(
                [new TextContent("Let me check the executions. "), ToolCall("call-1", nameof(DbQueryTools.GetRuns), ("flowId", 0))],
                [new TextContent("It failed.")]);

            await AskAsync();

            EventsOf(AiChatEventTypeEnum.DONE).Single().Text.ShouldBe("It failed.");
        }

        [Fact]
        public async Task Words_held_back_for_the_next_batch_are_never_lost()
        {
            UseModel([new TextContent("One "), new TextContent("two "), new TextContent("three.")]);

            await AskAsync();

            EventsOf(AiChatEventTypeEnum.DONE).Single().Text.ShouldBe("One two three.");
            "One two three.".ShouldStartWith(WrittenText());
        }

        [Fact]
        public async Task A_model_that_writes_nothing_is_a_failure_rather_than_an_empty_answer()
        {
            UseModel([new TextContent("  ")]);

            await AskAsync();

            EventsOf(AiChatEventTypeEnum.DONE).ShouldBeEmpty();
            EventsOf(AiChatEventTypeEnum.FAILED).Single().Error.ShouldBe("The model finished without writing an answer.");
        }

        [Fact]
        public async Task Nothing_is_asked_while_the_chat_is_not_available()
        {
            FakeStreamingChatClient model = UseModel([new TextContent("Hello.")]);
            _provider.IsConfigured = false;

            await AskAsync();

            model.Requests.ShouldBeEmpty();
            EventsOf(AiChatEventTypeEnum.FAILED).ShouldHaveSingleItem();
        }

        [Fact]
        public async Task A_stopped_answer_is_never_reported_as_done()
        {
            using CancellationTokenSource stop = new CancellationTokenSource();
            FakeStreamingChatClient model = UseModel([new TextContent("Half "), new TextContent("of it.")]);
            model.OnContent = x => stop.Cancel();

            await Should.ThrowAsync<OperationCanceledException>(() => AskAsync(ct: stop.Token));

            EventsOf(AiChatEventTypeEnum.DONE).ShouldBeEmpty();
        }

        // ================================================================
        // What it looked at
        // ================================================================

        [Fact]
        public async Task A_tool_is_said_in_plain_words_with_the_flow_by_name()
        {
            int executionId = AddFlowWithExecution("Price - Crude Oil", "Check number");
            int flowId;
            using (AppDbContext db = _database.CreateDbContext())
                flowId = db.Executions.Single(x => x.Id == executionId).FlowId;

            UseModel(
                [ToolCall("call-1", nameof(DbQueryTools.GetRuns), ("flowId", flowId))],
                [new TextContent("Done.")]);

            await AskAsync();

            AiChatEventDto started = EventsOf(AiChatEventTypeEnum.TOOL_STARTED).Single();
            started.Activity.ShouldBe("Listed the executions of Price - Crude Oil");
            EventsOf(AiChatEventTypeEnum.TOOL_FINISHED).Single().ActivityId.ShouldBe(started.ActivityId);
        }

        [Fact]
        public async Task A_step_read_from_an_execution_can_be_opened_under_the_name_it_executed_with()
        {
            int executionId = AddFlowWithExecution("Price - Crude Oil", "Check number");

            using (AppDbContext db = _database.CreateDbContext())
            {
                FlowStep renamed = db.FlowSteps.Single();
                renamed.Name = "Check number exists";
                db.SaveChanges();
            }

            UseModel(
                [ToolCall("call-1", nameof(DbQueryTools.GetRunSteps), ("executionId", executionId))],
                [new TextContent("`Check number` failed.")]);

            await AskAsync();

            AiChatStepReferenceDto step = EventsOf(AiChatEventTypeEnum.TOOL_FINISHED).Single().Steps.Single();
            step.Name.ShouldBe("Check number");

            using AppDbContext read = _database.CreateDbContext();
            step.FlowStepId.ShouldBe(read.FlowSteps.Single().Id);
            step.FlowId.ShouldBe(read.Flows.Single().Id);
        }

        [Fact]
        public async Task A_step_deleted_since_has_nothing_to_open()
        {
            int executionId = AddFlowWithExecution("Price - Crude Oil", "Check number");

            using (AppDbContext db = _database.CreateDbContext())
            {
                db.FlowSteps.Remove(db.FlowSteps.Single());
                db.SaveChanges();
            }

            UseModel(
                [ToolCall("call-1", nameof(DbQueryTools.GetRunSteps), ("executionId", executionId))],
                [new TextContent("It failed.")]);

            await AskAsync();

            EventsOf(AiChatEventTypeEnum.TOOL_FINISHED).Single().Steps.ShouldBeEmpty();
        }

        // ================================================================
        // What is on screen
        // ================================================================

        [Fact]
        public async Task The_model_is_told_which_execution_the_user_is_looking_at()
        {
            int executionId = AddFlowWithExecution("Price - Crude Oil", "Check number");
            FakeStreamingChatClient model = UseModel([new TextContent("Done.")]);

            await AskAsync(executionId: executionId);

            string instructions = model.Requests.Single().First(x => x.Role == ChatRole.System).Text;
            instructions.ShouldContain($"execution {executionId} of the flow \"Price - Crude Oil\"");
        }

        [Fact]
        public async Task A_question_from_no_page_says_nothing_about_a_page()
        {
            FakeStreamingChatClient model = UseModel([new TextContent("Done.")]);

            await AskAsync();

            model.Requests.Single().First(x => x.Role == ChatRole.System).Text.ShouldNotContain("The user is looking at");
        }

        [Fact]
        public async Task Screenshots_go_with_the_question_when_asked_for()
        {
            int executionId = AddFlowWithExecution("Price - Crude Oil", "Check number");
            _screenshots.Images = [new AiImage { Label = "The screen.", Name = "Screen at \"Check number\"", Bytes = [1], MediaType = "image/jpeg" }];
            FakeStreamingChatClient model = UseModel([new TextContent("Done.")]);

            await AskAsync(executionId: executionId, sendScreenshots: true);

            EventsOf(AiChatEventTypeEnum.STARTED).Single().Images.ShouldBe(["Screen at \"Check number\""]);
            model.Requests.Single().Last(x => x.Role == ChatRole.User).Contents.OfType<DataContent>().ShouldHaveSingleItem();
        }

        [Fact]
        public async Task A_follow_up_about_the_same_execution_leaves_the_screenshots_out()
        {
            int executionId = AddFlowWithExecution("Price - Crude Oil", "Check number");
            _screenshots.Images = [new AiImage { Label = "The screen.", Name = "Screen", Bytes = [1], MediaType = "image/jpeg" }];
            FakeStreamingChatClient model = UseModel([new TextContent("Done.")]);

            await AskAsync(executionId: executionId, sendScreenshots: false);

            EventsOf(AiChatEventTypeEnum.STARTED).Single().Images.ShouldBeEmpty();
            model.Requests.Single().Last(x => x.Role == ChatRole.User).Contents.OfType<DataContent>().ShouldBeEmpty();
        }

        [Fact]
        public async Task A_cloud_model_not_allowed_the_screen_is_never_sent_screenshots()
        {
            int executionId = AddFlowWithExecution("Price - Crude Oil", "Check number");
            _screenshots.Images = [new AiImage { Label = "The screen.", Name = "Screen", Bytes = [1], MediaType = "image/jpeg" }];
            _provider.CanSendScreenData = false;
            FakeStreamingChatClient model = UseModel([new TextContent("Done.")]);

            await AskAsync(executionId: executionId, sendScreenshots: true);

            EventsOf(AiChatEventTypeEnum.STARTED).Single().Images.ShouldBeEmpty();
            model.Requests.Single().Last(x => x.Role == ChatRole.User).Contents.OfType<DataContent>().ShouldBeEmpty();
        }

        // ================================================================
        // Private types
        // ================================================================

        private sealed class FakeModelService : IAiModelService
        {
            public Task<AiModelsDto> GetModelsAsync(CancellationToken ct = default)
            {
                throw new NotSupportedException();
            }

            public Task<IReadOnlyList<AiModelSuggestionDto>> GetModelSuggestionsAsync(CancellationToken ct = default)
            {
                throw new NotSupportedException();
            }

            public Task<bool> StartModelDownloadAsync(string model, CancellationToken ct = default)
            {
                throw new NotSupportedException();
            }

            public Task<bool> SupportsToolsAsync(CancellationToken ct = default)
            {
                return Task.FromResult(true);
            }

            public Task<bool> SupportsVisionAsync(CancellationToken ct = default)
            {
                return Task.FromResult(true);
            }

            public Task<AiLocalModelStatusDto> GetLocalModelStatusAsync(CancellationToken ct = default)
            {
                throw new NotSupportedException();
            }

            public Task<bool> FreeLocalModelAsync(CancellationToken ct = default)
            {
                throw new NotSupportedException();
            }
        }

        private sealed class FakeScreenshotReader : IExecutionScreenshotReader
        {
            public IReadOnlyList<AiImage> Images { get; set; } = [];

            public Task<IReadOnlyList<AiImage>> GetForExecutionAsync(int executionId, CancellationToken ct = default)
            {
                return Task.FromResult(Images);
            }

            public Task<bool> IsAllowedAsync(CancellationToken ct = default)
            {
                return Task.FromResult(true);
            }
        }

        private sealed class NoDocuments : IAiDocumentIndexService
        {
            public bool IsAvailable()
            {
                return false;
            }

            public IReadOnlyList<AiDocumentSearchResult> Search(string question, int count)
            {
                return [];
            }
        }
    }
}
