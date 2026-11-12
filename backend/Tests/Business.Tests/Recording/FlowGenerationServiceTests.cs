using System.Drawing;
using System.Text.Json;
using Business.Ai.AiDocuments;
using Business.Ai.ScriptRevision;
using Business.Flows.DataService;
using Business.FlowScript;
using Business.FlowScript.Scanner;
using Business.FlowScript.Text;
using Business.Recording.Draft;
using Business.Recording.Draft.Actions;
using Business.Recording.Generation;
using Business.Recording.Screens;
using Business.Recording.Setup;
using Business.Tests.Fakes;
using Core.Enums;
using Core.Models.Business;
using Core.Models.Business.AreaPointResolution;
using Core.Models.Database;
using Core.Models.Dtos;
using Core.Models.Dtos.FlowScript;
using DataAccess;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging.Abstractions;
using static Business.Tests.TestToken;

namespace Business.Tests.Recording
{
    /// <summary>
    /// The plain draft, end to end: a flow made by the recording setup, a reviewed recording added
    /// to it, written as a script and imported through a real database - or inserted at a position,
    /// as the editor's "Record steps here" does. Then the model's draft and the Check page's
    /// revisions, with the model played by the test.
    /// </summary>
    public sealed class FlowGenerationServiceTests : IDisposable
    {
        private const int CHROME = 9;

        private readonly TestDatabase _database = new TestDatabase();
        private readonly FakeAreaPointResolver _resolver = new FakeAreaPointResolver();
        private readonly FakeWindowService _windows = new FakeWindowService();
        private readonly FakeProcessService _processes = new FakeProcessService();
        private readonly DataService _dataService;
        private readonly RecordingSetupService _setup;
        private readonly FakeAiClientFactory _models = new FakeAiClientFactory();
        private readonly FakeAiProviderService _provider = new FakeAiProviderService();
        private readonly FakeOllamaService _ollama = new FakeOllamaService();
        private readonly FlowGenerationService _generation;

        private readonly RecordingResultDto _recording = new RecordingResultDto();
        private readonly ReviewedRecordingDto _reviewed = new ReviewedRecordingDto();
        private readonly List<int> _requestsWhenSent = [];

        public FlowGenerationServiceTests()
        {
            _dataService = TestDataService.For(_database);
            _setup = new RecordingSetupService(_dataService, _resolver, _windows, _processes);

            FlowScriptExporter exporter = new FlowScriptExporter(_database, new Printer());
            FlowScriptImporter importer = new FlowScriptImporter(new Scanner(), _dataService);
            RecordingDraftBuilder draftBuilder = new RecordingDraftBuilder(new FakeScreenshotService());
            ScriptRevisionService revision = new ScriptRevisionService(_models, new Scanner(), new Printer(), NullLogger<ScriptRevisionService>.Instance);
            RecordingWordsService words = new RecordingWordsService(_models, NullLogger<RecordingWordsService>.Instance);
            FlowExampleIndex examples = new FlowExampleIndex(new FakeEmbeddingService());
            RecordingScreenDescriber describer = new RecordingScreenDescriber(_models, _provider, _ollama, new FakeScreenshotService(), new FakeOpenCvService(), NullLogger<RecordingScreenDescriber>.Instance);
            _generation = new FlowGenerationService(exporter, importer, draftBuilder, new Printer(), new Scanner(), _dataService, revision, words, examples, _provider, describer);

            _resolver.AreasByName["chrome"] = AreaResolution.Ok(new Rectangle(0, 0, 800, 600), new WindowHandle(7));
            _windows.WindowProcessId = CHROME;
            _processes.ExecutablePaths[CHROME] = @"C:\Program Files\Google\Chrome\Application\chrome.exe";
        }

        public void Dispose()
        {
            _database.Dispose();
        }

        private async Task<int> NewFlowAsync(bool isQaTest = false)
        {
            RecordingSetupDto setup = new RecordingSetupDto
            {
                Name = "Download invoices",
                Description = "Logs in and downloads last month's invoices.",
                IsQaTest = isQaTest,
                ProcessName = "chrome",
                ScalesWith = ScalesWithEnum.DPI,
                AddsLaunchStep = true,
            };

            ResultDto<int> created = await _setup.CreateFlowAsync(setup, Ct);
            created.IsSuccess.ShouldBeTrue(created.ErrorMessage);

            return created.Data;
        }

        private void Record()
        {
            _recording.Actions.Add(new RecordedActionDto { Index = 0, Kind = RecordedActionKindEnum.CLICK, LocationX = 400, LocationY = 300, ScreenshotIndex = 0, WaitedMilliseconds = 1200, Window = RecordedWindowEnum.MAIN_AREA });
            _recording.Actions.Add(new RecordedActionDto { Index = 1, Kind = RecordedActionKindEnum.TYPING, Text = "ops@example.com" });
            _recording.ClickedElements.Add(new ClickedElementDto { ActionIndex = 0, X = 380, Y = 290, Width = 40, Height = 20, Text = "Email", FoundBy = ElementFoundByEnum.MODEL, Image = [2] });

            _reviewed.Actions.Add(new ReviewedActionDto { ActionIndex = 0, Choice = ReviewChoiceEnum.WAIT_AND_CLICK });
            _reviewed.Actions.Add(new ReviewedActionDto { ActionIndex = 1, Choice = ReviewChoiceEnum.INPUT, InputName = "email" });
        }

        private static RecordingScreenDto ScreenAt(int actionIndex)
        {
            return new RecordingScreenDto { ActionIndex = 0, Image = [1], X = 0, Y = 0, Width = 800, Height = 600, Dpi = 96 };
        }

        private async Task<FlowGenerationResultDto> ImportPlainDraftAsync(int flowId)
        {
            ResultDto<FlowGenerationResultDto> imported = await _generation.ImportPlainDraftAsync(flowId, _recording, _reviewed, ScreenAt, Ct);
            imported.IsSuccess.ShouldBeTrue(imported.ErrorMessage);

            return imported.Data!;
        }

        [Fact]
        public async Task A_reviewed_recording_becomes_its_flow_after_the_launch_step()
        {
            int flowId = await NewFlowAsync();
            Record();

            FlowScriptImportResultDto imported = (await ImportPlainDraftAsync(flowId)).Import;

            imported.IsSuccess.ShouldBeTrue();
            imported.Errors.ShouldBeEmpty();
            imported.FlowId.ShouldBe(flowId);
            imported.MissingTemplates.ShouldBeEmpty();

            using AppDbContext db = _database.CreateDbContext();
            List<FlowStep> roots = await db.FlowSteps.Where(x => x.RootId == flowId && x.ParentFlowStepId == null).OrderBy(x => x.OrderNumber).ToListAsync(Ct);
            roots.Select(x => x.FlowStepType).ShouldBe([FlowStepTypeEnum.SYSTEM_COMMAND, FlowStepTypeEnum.SEARCH_IMAGE]);
            roots[1].Name.ShouldBe("Find Email");

            FlowStepTemplate template = await db.FlowStepTemplates.Include(x => x.FlowTemplate).SingleAsync(x => x.FlowStepId == roots[1].Id, Ct);
            template.FlowTemplate.TemplateImage.ShouldNotBeNull();

            FlowInput input = await db.FlowInputs.SingleAsync(x => x.FlowId == flowId, Ct);
            (input.Name, input.DefaultValue).ShouldBe(("email", "ops@example.com"));
        }

        // The import replaces the flow's rows and keeps the row itself, so what the setup said stays.
        [Fact]
        public async Task The_flow_keeps_what_the_setup_said_about_it()
        {
            int flowId = await NewFlowAsync();
            Record();

            await ImportPlainDraftAsync(flowId);

            using AppDbContext db = _database.CreateDbContext();
            Flow flow = await db.Flows.SingleAsync(Ct);
            (flow.Id, flow.Name, flow.IsQaTest).ShouldBe((flowId, "Download invoices", false));
            flow.Description.ShouldBe("Logs in and downloads last month's invoices.");
            (await db.FlowAreas.SingleAsync(Ct)).IsMain.ShouldBeTrue();
        }

        // What the Check page shows: the flow read back, counted as a tester counts it.
        [Fact]
        public async Task The_plain_draft_says_what_was_saved()
        {
            int flowId = await NewFlowAsync();
            Record();

            FlowGenerationResultDto saved = await ImportPlainDraftAsync(flowId);

            saved.IsModelDraft.ShouldBeFalse();
            saved.Draft.ShouldBeEmpty();
            saved.Script.ShouldContain("Find Email");
            saved.StepCount.ShouldBe(Steps(flowId).Count(x => x.FlowStepType != FlowStepTypeEnum.SUCCESS && x.FlowStepType != FlowStepTypeEnum.FAILURE));
            (saved.InputCount, saved.TemplateCount).ShouldBe((1, 1));
        }

        // A QA test is generated by the model only, so its plain draft is refused rather than saved.
        [Fact]
        public async Task A_QA_test_is_refused_the_plain_draft_and_keeps_its_rows()
        {
            int flowId = await NewFlowAsync(isQaTest: true);
            List<int> before = Steps(flowId).Select(x => x.Id).Order().ToList();
            Record();

            ResultDto<FlowGenerationResultDto> imported = await _generation.ImportPlainDraftAsync(flowId, _recording, _reviewed, ScreenAt, Ct);

            imported.IsSuccess.ShouldBeFalse();
            imported.ErrorMessage.ShouldBe("A QA test is generated by the model only. Start Ollama with a model chosen in Settings, then generate it.");
            Steps(flowId).Select(x => x.Id).Order().ShouldBe(before);
        }

        // ================================================================
        // Into a flow that already existed
        // ================================================================

        // Only the new rows are written, so a step already there keeps its history.
        [Fact]
        public async Task A_recording_at_a_position_goes_in_between_and_the_steps_already_there_keep_their_ids()
        {
            int flowId = await NewFlowAsync();
            int launchId = Steps(flowId).Single(x => x.ParentFlowStepId == null).Id;
            Record();

            RecordingDraftPositionDto position = new RecordingDraftPositionDto { FlowId = flowId, Index = 0 };
            ResultDto<FlowScriptImportResultDto> inserted = await _generation.InsertPlainDraftAsync(position, _recording, _reviewed, ScreenAt, Ct);

            inserted.IsSuccess.ShouldBeTrue(inserted.ErrorMessage);
            FlowScriptImportResultDto result = inserted.Data.ShouldNotBeNull();
            (result.IsSuccess, result.FlowId).ShouldBe((true, flowId));

            List<FlowStep> steps = Steps(flowId);
            List<FlowStep> roots = steps.Where(x => x.ParentFlowStepId == null).OrderBy(x => x.OrderNumber).ToList();
            roots.Select(x => x.Name).ShouldBe(["Find Email", "Launch chrome"]);
            roots[1].Id.ShouldBe(launchId);

            using AppDbContext db = _database.CreateDbContext();
            FlowStepTemplate template = await db.FlowStepTemplates.Include(x => x.FlowTemplate).SingleAsync(x => x.FlowStepId == roots[0].Id, Ct);
            template.FlowTemplate.TemplateImage.ShouldNotBeNull();
            FlowInput input = await db.FlowInputs.SingleAsync(x => x.FlowId == flowId, Ct);
            (input.Name, input.DefaultValue).ShouldBe(("email", "ops@example.com"));
        }

        // The area is new and its parent is not, so the parent is linked rather than written again.
        [Fact]
        public async Task A_click_found_by_its_words_brings_its_area_into_the_flow_it_goes_into()
        {
            int flowId = await NewFlowAsync();
            Record();
            (_reviewed.Actions[0].SearchBy, _reviewed.Actions[0].SearchText) = (SearchByEnum.TEXT, "Email");

            RecordingDraftPositionDto position = new RecordingDraftPositionDto { FlowId = flowId, Index = 0 };
            ResultDto<FlowScriptImportResultDto> inserted = await _generation.InsertPlainDraftAsync(position, _recording, _reviewed, ScreenAt, Ct);

            inserted.IsSuccess.ShouldBeTrue(inserted.ErrorMessage);

            using AppDbContext db = _database.CreateDbContext();
            List<FlowArea> areas = await db.FlowAreas.Where(x => x.FlowId == flowId).ToListAsync(Ct);
            FlowArea main = areas.Single(x => x.IsMain);
            areas.Count.ShouldBe(2);
            FlowArea read = areas.Single(x => !x.IsMain);
            (read.Name, read.ParentFlowAreaId).ShouldBe(("Find Email area", main.Id));

            FlowStep search = Steps(flowId).Single(x => x.FlowStepType == FlowStepTypeEnum.SEARCH_TEXT);
            (search.ConditionText, search.FlowAreaId).ShouldBe(("Email", read.Id));
        }

        [Fact]
        public async Task A_recording_under_a_branch_goes_in_that_branch()
        {
            int flowId = await NewFlowAsync();
            FlowStep check = new FlowStep { RootId = flowId, FlowId = flowId, OrderNumber = 1, FlowStepType = FlowStepTypeEnum.SEARCH_IMAGE, Name = "Signed in" };
            int checkId = (await _dataService.FlowStep.CreateAsync(check, [], Ct)).Data;
            int successId = Steps(flowId).Single(x => x.ParentFlowStepId == checkId && x.FlowStepType == FlowStepTypeEnum.SUCCESS).Id;
            Record();

            RecordingDraftPositionDto position = new RecordingDraftPositionDto { FlowId = flowId, ParentFlowStepId = successId, Index = 0 };
            ResultDto<FlowScriptImportResultDto> inserted = await _generation.InsertPlainDraftAsync(position, _recording, _reviewed, ScreenAt, Ct);

            inserted.IsSuccess.ShouldBeTrue(inserted.ErrorMessage);
            Steps(flowId).Where(x => x.ParentFlowStepId == successId).Select(x => x.Name).ShouldBe(["Find Email"]);
        }

        [Fact]
        public async Task A_position_under_a_step_no_longer_there_writes_nothing_and_says_so()
        {
            int flowId = await NewFlowAsync();
            int stepCount = Steps(flowId).Count;
            Record();

            RecordingDraftPositionDto position = new RecordingDraftPositionDto { FlowId = flowId, ParentFlowStepId = 999, Index = 0 };
            ResultDto<FlowScriptImportResultDto> inserted = await _generation.InsertPlainDraftAsync(position, _recording, _reviewed, ScreenAt, Ct);

            inserted.ErrorMessage.ShouldBe("The step the recording goes under is no longer in the flow.");
            Steps(flowId).Count.ShouldBe(stepCount);
        }

        // ================================================================
        // The model's draft
        // ================================================================

        private const string PROPOSE_TEST = "You help turn a recording into a QA test";

        // Answers the words for the draft's searches, and for a QA test the stages and checks, as
        // the test says.
        private FakeChatClient Model(object names, object? test = null)
        {
            FakeChatClient model = new FakeChatClient(messages =>
            {
                bool isTest = messages.First(x => x.Role == ChatRole.System).Text.StartsWith(PROPOSE_TEST, StringComparison.Ordinal);
                object answer = isTest ? test ?? new { stages = Array.Empty<object>(), checks = Array.Empty<object>() } : names;

                return answer as string ?? JsonSerializer.Serialize(answer);
            });

            _models.Client = model;
            return model;
        }

        // The words for the one search the recording has.
        private static object Named(string name, string comment = "", string notFound = "", string[]? notes = null, string[]? questions = null)
        {
            return new
            {
                steps = new[] { new { id = "S1", name, comment, notFound } },
                questions = questions ?? [],
                notes = notes ?? [],
            };
        }

        private static string RequestOf(FakeChatClient model)
        {
            return model.Requests[0].First(x => x.Role == ChatRole.User).Text;
        }

        private Task<FlowGenerationResultDto> GenerateAsync(int flowId, List<FlowGenerationProgressDto> progress)
        {
            return _generation.ImportModelDraftAsync(flowId, _recording, _reviewed, ScreenAt, progress.Add, SendAsync, Ct);
        }

        // The tester pressing Send at once, with how many requests the model had had by then.
        private Task SendAsync(CancellationToken ct)
        {
            FakeChatClient? model = _models.Client as FakeChatClient;
            _requestsWhenSent.Add(model?.Requests.Count ?? 0);

            return Task.CompletedTask;
        }

        [Fact]
        public async Task The_model_s_words_are_put_on_the_draft_and_imported_with_its_notes()
        {
            int flowId = await NewFlowAsync();
            Record();
            Model(Named("Find the email field", "Signs in with the work address.", "the sign-in form never showed", notes: ["Check the email field."]));
            List<FlowGenerationProgressDto> progress = [];

            FlowGenerationResultDto generated = await GenerateAsync(flowId, progress);

            generated.IsModelDraft.ShouldBeTrue();
            generated.Import.IsSuccess.ShouldBeTrue();
            generated.Notes.ShouldBe(["Check the email field."]);
            progress.Select(x => (x.Stage, x.Attempt)).ShouldBe(
            [
                (FlowGenerationStageEnum.LOOKING_AT_SCREENS, 0),
                (FlowGenerationStageEnum.LOOKING_AT_SCREENS, 0),
                (FlowGenerationStageEnum.WRITING_DRAFT, 0),
                (FlowGenerationStageEnum.ASKING_MODEL, 1),
                (FlowGenerationStageEnum.SAVING, 1),
            ]);

            List<FlowStep> steps = Steps(flowId);
            FlowStep search = steps.Single(x => x.FlowStepType == FlowStepTypeEnum.SEARCH_IMAGE);
            search.Name.ShouldBe("Find the email field");
            search.CodeComment.ShouldBe("Signs in with the work address.\nrecorded after a 1.2s wait");
            steps.Single(x => x.FlowStepType == FlowStepTypeEnum.END_EXECUTION).Message.ShouldBe("the sign-in form never showed");

            using AppDbContext db = _database.CreateDbContext();
            FlowStepTemplate template = await db.FlowStepTemplates.Include(x => x.FlowTemplate).SingleAsync(x => x.FlowStepId == search.Id, Ct);
            template.FlowTemplate.TemplateImage.ShouldNotBeNull();
            FlowInput input = await db.FlowInputs.SingleAsync(x => x.FlowId == flowId, Ct);
            input.DefaultValue.ShouldBe("ops@example.com");
        }

        // The Check page marks what the model changed against the draft, so nothing else may differ.
        [Fact]
        public async Task The_saved_flow_differs_from_the_draft_by_the_model_s_words_only()
        {
            int flowId = await NewFlowAsync();
            Record();
            Model(Named("Find the email field"));

            FlowGenerationResultDto generated = await GenerateAsync(flowId, []);

            generated.Draft.ShouldContain("Find Email");
            generated.Draft.Replace("Find Email", "Find the email field").ShouldBe(generated.Script);
        }

        // The model is sent the searches by id and never the script, so it has nothing to break.
        [Fact]
        public async Task The_model_is_sent_the_task_and_the_searches_and_never_the_script()
        {
            int flowId = await NewFlowAsync();
            Record();
            FakeChatClient model = Model(Named("Find the email field"));

            await GenerateAsync(flowId, []);

            string request = RequestOf(model);
            request.ShouldStartWith("The task, in the tester's words: Logs in and downloads last month's invoices.");
            request.ShouldContain("S1  Find Email");
            request.ShouldContain("then: click, type {{email}}");
            request.ShouldNotContain("Script Language:");
            request.ShouldNotContain("Templates:");
            request.ShouldNotContain("ops@example.com");
            model.Requests[0].First(x => x.Role == ChatRole.System).Text.ShouldNotContain("Script Language");
        }

        // The tester always gets a flow.
        [Fact]
        public async Task When_no_answer_reads_the_plain_draft_is_saved_and_says_why()
        {
            int flowId = await NewFlowAsync();
            Record();
            Model("I cannot help with that.");

            FlowGenerationResultDto generated = await GenerateAsync(flowId, []);

            generated.IsModelDraft.ShouldBeFalse();
            generated.Reason.ShouldBe($"No answer from the model read as the JSON asked for in {RecordingWordsService.ATTEMPT_COUNT} attempts.");
            generated.Import.IsSuccess.ShouldBeTrue();
            Steps(flowId).ShouldContain(x => x.Name == "Find Email");
        }

        // A field that will not do keeps the draft's; the rest of the answer is still taken.
        [Fact]
        public async Task A_name_too_long_keeps_the_draft_s_and_the_rest_is_taken()
        {
            int flowId = await NewFlowAsync();
            Record();
            Model(Named(new string('x', 80), "Signs in.", "the sign-in form never showed"));

            FlowGenerationResultDto generated = await GenerateAsync(flowId, []);

            generated.IsModelDraft.ShouldBeTrue();
            generated.NotTaken.ShouldBe(["The name for S1 was longer than a line, so it is still \"Find Email\"."]);
            FlowStep search = Steps(flowId).Single(x => x.FlowStepType == FlowStepTypeEnum.SEARCH_IMAGE);
            (search.Name, search.CodeComment).ShouldBe(("Find Email", "Signs in.\nrecorded after a 1.2s wait"));
        }

        // Steps, areas, points and inputs share one set of names.
        [Fact]
        public async Task A_name_already_taken_is_numbered_rather_than_refused()
        {
            int flowId = await NewFlowAsync();
            Record();
            Model(Named("Launch chrome"));

            await GenerateAsync(flowId, []);

            Steps(flowId).Single(x => x.FlowStepType == FlowStepTypeEnum.SEARCH_IMAGE).Name.ShouldBe("Launch chrome 2");
        }

        [Fact]
        public async Task The_model_s_questions_reach_the_check_page()
        {
            int flowId = await NewFlowAsync();
            Record();
            Model(Named("Find the email field", questions: ["Should it sign in with a different account each time?"]));

            FlowGenerationResultDto generated = await GenerateAsync(flowId, []);

            generated.Questions.ShouldBe(["Should it sign in with a different account each time?"]);
        }

        // The notes are screen text, and the flow is written into a repository.
        [Fact]
        public async Task The_model_is_told_what_changed_on_the_screen_and_the_saved_flow_keeps_none_of_it()
        {
            int flowId = await NewFlowAsync();
            Record();
            _recording.ScreenChanges.Add(new ScreenChangeDto { ActionIndex = 0, NextActionIndex = 2, Appeared = ["Welcome back"], Gone = ["Sign in to continue"], ChangedShare = 0.3 });
            FakeChatClient model = Model(Named("Find the email field"));

            FlowGenerationResultDto generated = await GenerateAsync(flowId, []);

            RequestOf(model).ShouldContain("screen: after the click appeared \"Welcome back\"");
            generated.IsModelDraft.ShouldBeTrue();
            generated.Draft.ShouldNotContain("screen:");
            generated.Script.ShouldNotContain("screen:");
            Steps(flowId).ShouldAllBe(x => !x.CodeComment.Contains("screen:"));
        }

        [Fact]
        public async Task Without_screen_content_the_notes_say_only_where_a_page_began()
        {
            int flowId = await NewFlowAsync();
            Record();
            _recording.ScreenChanges.Add(new ScreenChangeDto { ActionIndex = 0, NextActionIndex = 2, Appeared = ["Welcome back"], ChangedShare = 0.9, IsNewScreen = true });
            _provider.CanSendScreenData = false;
            FakeChatClient model = Model(Named("Find the email field"));

            await GenerateAsync(flowId, []);

            string request = RequestOf(model);
            request.ShouldNotContain("Welcome back");
            request.ShouldContain("screen: after the click, a new page");
        }

        // The screenshot goes to the local model only; what it says goes to the one writing the words.
        [Fact]
        public async Task What_the_local_model_sees_of_the_screen_reaches_the_model_as_a_note()
        {
            int flowId = await NewFlowAsync();
            Record();
            _ollama.Capabilities.Add("vision");
            FakeChatClient local = new FakeChatClient(messages => "The sign-in page of the invoices site.");
            _models.LocalClient = local;
            FakeChatClient model = Model(Named("Find the email field"));
            List<FlowGenerationProgressDto> progress = [];

            await GenerateAsync(flowId, progress);

            local.Requests.ShouldHaveSingleItem().Single().Contents.OfType<DataContent>().ShouldHaveSingleItem();
            RequestOf(model).ShouldContain("screen: the local model sees the page as \"The sign-in page of the invoices site.\"");
            model.Requests[0].ShouldAllBe(x => !x.Contents.OfType<DataContent>().Any());
            (progress[^1].LookedAt, progress[^1].ToLookAt).ShouldBe((1, 1));
        }

        [Fact]
        public async Task Without_screen_content_the_local_model_is_not_asked_to_look()
        {
            int flowId = await NewFlowAsync();
            Record();
            _ollama.Capabilities.Add("vision");
            FakeChatClient local = new FakeChatClient(messages => "The sign-in page of the invoices site.");
            _models.LocalClient = local;
            _provider.CanSendScreenData = false;
            Model(Named("Find the email field"));

            await GenerateAsync(flowId, []);

            local.Requests.ShouldBeEmpty();
        }

        // A window's title and an element's name are screen text; what kind of element it was is not.
        [Fact]
        public async Task Without_screen_content_the_model_is_sent_no_title_and_no_name_Windows_had()
        {
            int flowId = await NewFlowAsync();
            Record();
            _recording.ScreenChanges.Add(new ScreenChangeDto { ActionIndex = 0, NextActionIndex = 2, IsNewTitle = true, Title = "Inbox - ops@example.com" });
            _recording.PressedElements.Add(new PressedElementDto { ActionIndex = 0, Role = "text field", Name = "Email address", Inside = ["group \"Account details\""] });
            _provider.CanSendScreenData = false;
            FakeChatClient model = Model(Named("Find the email field"));

            await GenerateAsync(flowId, []);

            string request = RequestOf(model);
            request.ShouldNotContain("ops@example.com");
            request.ShouldNotContain("Email address");
            request.ShouldNotContain("Account details");
            request.ShouldContain("screen: what was pressed, as Windows names it: text field");
            request.ShouldContain("screen: after the click, a new page");
        }

        // A cloud provider not allowed screen content names the steps from the description.
        [Fact]
        public async Task Without_screen_content_the_model_is_sent_no_read_text_and_told_the_names_are_placeholders()
        {
            int flowId = await NewFlowAsync();
            Record();
            _provider.CanSendScreenData = false;
            FakeChatClient model = Model(Named("Find the email field"));
            List<FlowGenerationProgressDto> progress = [];

            await GenerateAsync(flowId, progress);

            string request = RequestOf(model);
            request.ShouldNotContain("Find Email");
            request.ShouldContain("Find icon");
            model.Requests[0].First(x => x.Role == ChatRole.System).Text.ShouldContain("placeholders");
            progress.ShouldAllBe(x => !x.IsReadTextSent);
        }

        [Fact]
        public async Task A_cloud_model_is_sent_what_was_read_off_the_screen_only_once_the_tester_sends_it()
        {
            int flowId = await NewFlowAsync();
            Record();
            _recording.ScreenChanges.Add(new ScreenChangeDto { ActionIndex = 0, NextActionIndex = 2, Appeared = ["Welcome back"], ChangedShare = 0.3 });
            _provider.IsCloudModelInUse = true;
            FakeChatClient model = Model(Named("Find the email field"));
            List<FlowGenerationProgressDto> progress = [];

            FlowGenerationResultDto generated = await GenerateAsync(flowId, progress);

            _requestsWhenSent.ShouldBe([0]);
            FlowGenerationProgressDto waiting = progress.Single(x => x.Stage == FlowGenerationStageEnum.WAITING_TO_SEND);
            waiting.Request.ShouldBe(RequestOf(model));

            string[] lines = waiting.Request.Split('\n');
            List<string> marked = waiting.RequestScreenLines.Select(x => lines[x - 1]).ToList();
            marked.ShouldContain(x => x.Contains("screen: after the click appeared \"Welcome back\""));
            marked.ShouldContain(x => x.Contains("Find Email"));
            waiting.Request.ShouldContain("Logs in and downloads");
            marked.ShouldNotContain(x => x.Contains("Logs in and downloads"));

            generated.RequestScreenLines.ShouldBe(waiting.RequestScreenLines);
            progress.Where(x => x.Stage != FlowGenerationStageEnum.WAITING_TO_SEND).ShouldAllBe(x => x.Request.Length == 0);
        }

        [Fact]
        public async Task A_request_never_sent_asks_nothing_and_saves_nothing()
        {
            int flowId = await NewFlowAsync();
            Record();
            _provider.IsCloudModelInUse = true;
            FakeChatClient model = Model(Named("Find the email field"));
            int stepCount = Steps(flowId).Count;

            await Should.ThrowAsync<OperationCanceledException>(() => _generation.ImportModelDraftAsync(flowId, _recording, _reviewed, ScreenAt, null, x => Task.FromCanceled(new CancellationToken(true)), Ct));

            model.Requests.ShouldBeEmpty();
            Steps(flowId).Count.ShouldBe(stepCount);
        }

        // Nothing leaves this machine for the local model; what was read is still marked, after.
        [Fact]
        public async Task The_local_model_is_sent_it_at_once_with_the_lines_marked_after()
        {
            int flowId = await NewFlowAsync();
            Record();
            Model(Named("Find the email field"));
            List<FlowGenerationProgressDto> progress = [];

            FlowGenerationResultDto generated = await GenerateAsync(flowId, progress);

            _requestsWhenSent.ShouldBeEmpty();
            progress.ShouldNotContain(x => x.Stage == FlowGenerationStageEnum.WAITING_TO_SEND);
            generated.RequestScreenLines.ShouldNotBeEmpty();
        }

        // Nothing read off the screen is in it, so there is nothing to read first.
        [Fact]
        public async Task Without_screen_content_a_cloud_model_is_sent_it_at_once_with_nothing_marked()
        {
            int flowId = await NewFlowAsync();
            Record();
            _recording.ScreenChanges.Add(new ScreenChangeDto { ActionIndex = 0, NextActionIndex = 2, Appeared = ["Welcome back"], ChangedShare = 0.9, IsNewScreen = true });
            _provider.IsCloudModelInUse = true;
            _provider.CanSendScreenData = false;
            Model(Named("Find the email field"));

            FlowGenerationResultDto generated = await GenerateAsync(flowId, []);

            _requestsWhenSent.ShouldBeEmpty();
            generated.IsModelDraft.ShouldBeTrue();
            generated.RequestScreenLines.ShouldBeEmpty();
        }

        // Review filled them in from what was read, and a Wait For Text is nothing without them.
        [Fact]
        public async Task Without_screen_content_the_words_Review_searched_by_are_still_shown_first()
        {
            int flowId = await NewFlowAsync();
            Record();
            (_reviewed.Actions[0].SearchBy, _reviewed.Actions[0].SearchText) = (SearchByEnum.TEXT, "Email");
            _provider.IsCloudModelInUse = true;
            _provider.CanSendScreenData = false;
            Model(Named("Find the email field"));
            List<FlowGenerationProgressDto> progress = [];

            await GenerateAsync(flowId, progress);

            _requestsWhenSent.ShouldBe([0]);
            FlowGenerationProgressDto waiting = progress.Single(x => x.Stage == FlowGenerationStageEnum.WAITING_TO_SEND);
            string[] lines = waiting.Request.Split('\n');
            waiting.RequestScreenLines.Select(x => lines[x - 1]).ShouldAllBe(x => x.Contains("Email", StringComparison.OrdinalIgnoreCase));
        }

        // A search found by its words reads them in an area of its own, which takes the search's new name.
        [Fact]
        public async Task A_search_by_its_words_takes_its_area_along_when_it_is_renamed()
        {
            int flowId = await NewFlowAsync();
            Record();
            (_reviewed.Actions[0].SearchBy, _reviewed.Actions[0].SearchText) = (SearchByEnum.TEXT, "Email");
            Model(Named("Find the email field"));

            FlowGenerationResultDto generated = await GenerateAsync(flowId, []);

            generated.IsModelDraft.ShouldBeTrue();
            using AppDbContext db = _database.CreateDbContext();
            (await db.FlowAreas.Where(x => x.FlowId == flowId && !x.IsMain).Select(x => x.Name).SingleAsync(Ct)).ShouldBe("Find the email field area");
        }

        // ================================================================
        // A QA test's draft
        // ================================================================

        [Fact]
        public async Task An_automation_is_asked_for_its_words_only()
        {
            int flowId = await NewFlowAsync();
            Record();
            FakeChatClient model = Model(Named("Find the email field"));

            FlowGenerationResultDto generated = await GenerateAsync(flowId, []);

            generated.IsModelDraft.ShouldBeTrue();
            model.Requests.ShouldHaveSingleItem();
            Steps(flowId).ShouldNotContain(x => x.FlowStepType == FlowStepTypeEnum.STAGE_MARKER);
        }

        // Whatever the model said, a QA test is divided into stages and says when it has passed.
        [Fact]
        public async Task A_QA_test_always_has_a_stage_and_a_pass()
        {
            int flowId = await NewFlowAsync(isQaTest: true);
            Record();
            FakeChatClient model = Model(Named("Find the email field"));

            FlowGenerationResultDto generated = await GenerateAsync(flowId, []);

            model.Requests.Count.ShouldBe(2);
            generated.IsModelDraft.ShouldBeTrue();
            List<FlowStep> steps = Steps(flowId);
            steps.ShouldContain(x => x.FlowStepType == FlowStepTypeEnum.STAGE_MARKER && x.Name == "Download invoices" && x.ParentFlowStepId == null);
            steps.ShouldContain(x => x.FlowStepType == FlowStepTypeEnum.END_EXECUTION && x.EndExecutionAsSuccess);

            using AppDbContext db = _database.CreateDbContext();
            (await db.Flows.SingleAsync(Ct)).IsQaTest.ShouldBeTrue();
        }

        [Fact]
        public async Task A_QA_test_s_stages_go_before_the_searches_the_model_names_them_at()
        {
            int flowId = await NewFlowAsync(isQaTest: true);
            Record();
            Model(Named("Find the email field"), new { stages = new[] { new { before = "S1", name = "Sign in", comment = "Proves the sign-in form takes the address." } }, checks = Array.Empty<object>() });

            await GenerateAsync(flowId, []);

            List<FlowStep> roots = Steps(flowId).Where(x => x.ParentFlowStepId == null).OrderBy(x => x.OrderNumber).ToList();
            roots.Select(x => x.Name).ShouldBe(["Launch chrome", "Sign in", "Find the email field"]);
            roots[1].CodeComment.ShouldBe("Proves the sign-in form takes the address.");
        }

        // What it tests is the tester's to say, so a stage of theirs stands and the model's goes elsewhere.
        [Fact]
        public async Task A_stage_the_tester_named_reaches_the_model_and_stands()
        {
            int flowId = await NewFlowAsync(isQaTest: true);
            Record();
            _reviewed.Actions[0].StageName = "Sign in";
            FakeChatClient model = Model(Named("Find the email field"), new { stages = new[] { new { before = "S1", name = "Log in", comment = "" } }, checks = Array.Empty<object>() });

            FlowGenerationResultDto generated = await GenerateAsync(flowId, []);

            RequestOf(model).ShouldContain("## Sign in");
            Steps(flowId).Where(x => x.FlowStepType == FlowStepTypeEnum.STAGE_MARKER).Select(x => x.Name).ShouldBe(["Sign in"]);
            generated.NotTaken.ShouldBe(["A stage already begins at S1, so \"Log in\" was left out."]);
        }

        private const string WRONG_VALUE = "not-the-password";

        private void RecordWrongValue()
        {
            _recording.Actions.Add(new RecordedActionDto { Index = 2, Kind = RecordedActionKindEnum.TYPING, Text = WRONG_VALUE });
            _reviewed.Actions.Add(new ReviewedActionDto { ActionIndex = 2, Choice = ReviewChoiceEnum.WRONG_ON_PURPOSE });
        }

        // The model never writes a step, so a value typed wrong on purpose stays exactly as it was.
        [Fact]
        public async Task A_value_wrong_on_purpose_reaches_the_model_as_such_and_stays_typed_as_it_is()
        {
            int flowId = await NewFlowAsync(isQaTest: true);
            Record();
            RecordWrongValue();
            FakeChatClient model = Model(Named("Find the email field"));

            FlowGenerationResultDto generated = await GenerateAsync(flowId, []);

            RequestOf(model).ShouldContain($"type \"{WRONG_VALUE}\" ({TypingDraftBuilder.WRONG_ON_PURPOSE_COMMENT})");
            generated.IsModelDraft.ShouldBeTrue();
            Steps(flowId).ShouldContain(x => x.KeyboardInputText == WRONG_VALUE && x.CodeComment == TypingDraftBuilder.WRONG_ON_PURPOSE_COMMENT);
        }

        // ================================================================
        // Checks the model added
        // ================================================================

        private static object Checking(params (string Words, string Name)[] checks)
        {
            return new
            {
                stages = Array.Empty<object>(),
                checks = checks.Select(x => new { after = "S1", words = x.Words, name = x.Name, comment = "Proves the sign-in worked.", notFound = $"{x.Words} never showed" }).ToArray(),
            };
        }

        [Fact]
        public async Task A_check_for_words_that_appeared_after_its_click_is_saved_and_offered_to_keep_or_drop()
        {
            int flowId = await NewFlowAsync(isQaTest: true);
            Record();
            _recording.ScreenChanges.Add(new ScreenChangeDto { ActionIndex = 0, Appeared = ["Welcome back"], ChangedShare = 0.3 });
            _recording.ScreenLines.AddRange(["Email", "Welcome back"]);
            Model(Named("Find Email"), Checking(("\"Welcome back\"", "Signed in")));

            FlowGenerationResultDto generated = await GenerateAsync(flowId, []);

            generated.IsModelDraft.ShouldBeTrue();
            ProposedCheckDto proposed = generated.ProposedChecks.ShouldHaveSingleItem();
            (proposed.Name, proposed.Under, proposed.IsReadOnScreen).ShouldBe(("Signed in", "Find Email", true));
            generated.Script.Split('\n')[proposed.Line - 1].ShouldContain("<[ Signed in ]>   contains <[ Welcome back ]>");
            Steps(flowId).Single(x => x.Name == "Signed in").CodeComment.ShouldBe("Proves the sign-in worked.");
        }

        // Words that never appeared after that click would fail the first execution.
        [Fact]
        public async Task A_check_for_words_that_did_not_appear_after_its_click_is_left_out_and_said()
        {
            int flowId = await NewFlowAsync(isQaTest: true);
            Record();
            _recording.ScreenLines.AddRange(["Email", "Welcome back"]);
            Model(Named("Find Email"), Checking(("Welcome back", "Signed in")));

            FlowGenerationResultDto generated = await GenerateAsync(flowId, []);

            generated.IsModelDraft.ShouldBeTrue();
            generated.ProposedChecks.ShouldBeEmpty();
            generated.NotTaken.ShouldBe(["A check for \"Welcome back\" after S1 was left out: no screenshot showed those words appear after that click."]);
        }

        [Fact]
        public async Task A_dropped_check_leaves_the_flow_and_the_others_are_still_offered()
        {
            int flowId = await NewFlowAsync(isQaTest: true);
            Record();
            _recording.ScreenChanges.Add(new ScreenChangeDto { ActionIndex = 0, Appeared = ["Welcome back", "Inbox"], ChangedShare = 0.3 });
            _recording.ScreenLines.AddRange(["Email", "Welcome back", "Inbox"]);
            Model(Named("Find Email"), Checking(("Welcome back", "Signed in"), ("Inbox", "Inbox shown")));
            await GenerateAsync(flowId, []);

            ProposedCheckDropDto drop = new ProposedCheckDropDto { FlowId = flowId, Name = "Signed in", Undecided = ["Signed in", "Inbox shown"] };
            ResultDto<FlowGenerationResultDto> dropped = await _generation.DropCheckAsync(drop, _recording.ScreenLines, Ct);

            dropped.IsSuccess.ShouldBeTrue(dropped.ErrorMessage);
            dropped.Data!.Script.ShouldNotContain("Signed in");
            dropped.Data.ProposedChecks.Select(x => x.Name).ShouldBe(["Inbox shown"]);
            Steps(flowId).ShouldNotContain(x => x.Name == "Signed in");
            Steps(flowId).ShouldContain(x => x.FlowStepType == FlowStepTypeEnum.KEYBOARD_INPUT);
        }

        // Answers with the script it was sent, changed as the test says, as Ask for changes is asked.
        private FakeChatClient ScriptModel(Func<string, string> change)
        {
            FakeChatClient model = new FakeChatClient(messages =>
            {
                string request = messages.First(x => x.Role == ChatRole.User).Text;
                string script = request[(request.IndexOf("The script:", StringComparison.Ordinal) + "The script:".Length)..].Trim();

                return JsonSerializer.Serialize(new { script = change(script), notes = new[] { "Check the email field." }, inputRenames = Array.Empty<object>() });
            });

            _models.Client = model;
            return model;
        }

        [Fact]
        public async Task Asked_for_changes_a_check_not_yet_kept_is_still_offered_and_a_kept_one_is_not()
        {
            int flowId = await NewFlowAsync(isQaTest: true);
            Record();
            _recording.ScreenChanges.Add(new ScreenChangeDto { ActionIndex = 0, Appeared = ["Welcome back"], ChangedShare = 0.3 });
            _recording.ScreenLines.AddRange(["Email", "Welcome back"]);
            Model(Named("Find Email"), Checking(("Welcome back", "Signed in")));
            await GenerateAsync(flowId, []);

            ScriptModel(x => x.Replace("Signed in", "Welcome shown"));
            FlowGenerationResultDto undecided = await _generation.ReviseAsync(flowId, "Say what the check is for.", ["Signed in"], _recording.ScreenLines, null, Ct);

            undecided.ProposedChecks.Select(x => x.Name).ShouldBe(["Welcome shown"]);

            ScriptModel(x => x);
            FlowGenerationResultDto kept = await _generation.ReviseAsync(flowId, "Leave it as it is.", [], _recording.ScreenLines, null, Ct);

            kept.ProposedChecks.ShouldBeEmpty();
        }

        // ================================================================
        // Ask for changes
        // ================================================================

        private async Task<int> PlainDraftAsync()
        {
            int flowId = await NewFlowAsync();
            Record();
            await ImportPlainDraftAsync(flowId);

            return flowId;
        }

        [Fact]
        public async Task Asked_for_changes_the_model_s_answer_replaces_the_flow()
        {
            int flowId = await PlainDraftAsync();
            FakeChatClient model = ScriptModel(x => x.Replace("Find Email", "Find the sign-in field"));
            List<FlowGenerationProgressDto> progress = [];

            FlowGenerationResultDto revised = await _generation.ReviseAsync(flowId, "Name the search after the sign-in.", [], [], progress.Add, Ct);

            (revised.IsModelDraft, revised.Import.IsSuccess).ShouldBe((true, true));
            revised.Draft.ShouldContain("Find Email");
            revised.Script.ShouldContain("Find the sign-in field");
            model.Requests[0].First(x => x.Role == ChatRole.User).Text.ShouldContain("Name the search after the sign-in.");
            progress.Select(x => x.Stage).ShouldBe([FlowGenerationStageEnum.WRITING_DRAFT, FlowGenerationStageEnum.ASKING_MODEL, FlowGenerationStageEnum.SAVING]);

            FlowStep search = Steps(flowId).Single(x => x.FlowStepType == FlowStepTypeEnum.SEARCH_IMAGE);
            search.Name.ShouldBe("Find the sign-in field");

            using AppDbContext db = _database.CreateDbContext();
            FlowStepTemplate template = await db.FlowStepTemplates.Include(x => x.FlowTemplate).SingleAsync(x => x.FlowStepId == search.Id, Ct);
            template.FlowTemplate.TemplateImage.ShouldNotBeNull();
            FlowInput input = await db.FlowInputs.SingleAsync(x => x.FlowId == flowId, Ct);
            input.DefaultValue.ShouldBe("ops@example.com");
        }

        [Fact]
        public async Task An_answer_that_never_reads_cleanly_leaves_the_flow_as_it_was()
        {
            int flowId = await PlainDraftAsync();
            List<int> before = Steps(flowId).Select(x => x.Id).Order().ToList();
            ScriptModel(x => x + "\nFnid Image  <[ Broken ]>\n");

            FlowGenerationResultDto revised = await _generation.ReviseAsync(flowId, "Sign out at the end.", [], [], null, Ct);

            revised.IsModelDraft.ShouldBeFalse();
            revised.Reason.ShouldBe($"No answer from the model read cleanly in {ScriptRevisionService.ATTEMPT_COUNT} attempts, so the flow is as it was.");
            revised.Refused.ShouldNotBeEmpty();
            revised.Script.ShouldBeEmpty();
            Steps(flowId).Select(x => x.Id).Order().ShouldBe(before);
        }

        private List<FlowStep> Steps(int flowId)
        {
            using AppDbContext db = _database.CreateDbContext();
            return db.FlowSteps.AsNoTracking().Where(x => x.RootId == flowId).ToList();
        }
    }
}
