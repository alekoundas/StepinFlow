using Business.Ai.Providers;
using Business.FlowScript.Models;
using Business.FlowScript.Scanner;
using Business.FlowScript.Text;
using Business.Recording.Draft;
using Business.Recording.Draft.Models;
using Business.Recording.Generation;
using Business.Recording.Generation.Models;
using Business.Tests.Fakes;
using Core.Enums;
using Core.Models.Database;
using Core.Models.Dtos;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging.Abstractions;
using OllamaSharp;
using static Business.Tests.TestToken;

namespace Business.Tests.Recording
{
    /// <summary>
    /// A real recording, as the recorder read it on 2026-11-10: signing in to Swag Labs at 4K in
    /// Firefox by copying the username and the password shown on the page, opening the menu and
    /// signing out. Every click's element was a box around the click, so the text read on it was
    /// cut - "andard cked_ou", "pgout" - which is what named the steps then. The plain draft is held
    /// to reading well without a model; the local model is asked for its words by a test left out
    /// unless -p:Model=true, since it needs Ollama with qwen3.5:4b.
    /// </summary>
    public sealed class SwagLabsRecordingTests
    {
        private const string MODEL = "qwen3.5:4b";
        private const string OLLAMA = "http://localhost:11434";

        private const string FIREFOX = "Swag Labs — Mozilla Firefox";
        private static readonly string[] IN_PAGE = ["document \"Swag Labs\"", $"window \"{FIREFOX}\""];
        private static readonly string[] IN_FORM = ["group \"Login\"", "document \"Swag Labs\"", $"window \"{FIREFOX}\""];

        private readonly RecordingResultDto _recording = new RecordingResultDto();
        private readonly ReviewedRecordingDto _reviewed = new ReviewedRecordingDto();

        public SwagLabsRecordingTests()
        {
            Press(RecordedActionKindEnum.CLICK, "andard cked_ou", "standard_user", "text", "standard_user", IN_PAGE, isDouble: true, page: 1);
            Copy();
            Press(RecordedActionKindEnum.CLICK, "ser", "Username", "text field", "Username", IN_FORM, isDouble: true, page: 1);
            Press(RecordedActionKindEnum.DRAG, "er", "Username", "text field", "Username", IN_FORM, page: 1);
            Paste();
            Press(RecordedActionKindEnum.CLICK, "ecre si", "secret_sauce", "text", "secret_sauce", IN_PAGE, isDouble: true, page: 1);
            Copy();
            Press(RecordedActionKindEnum.DRAG, "•", "", "text field", "Password", IN_FORM, page: 1);
            Paste();
            int login = Press(RecordedActionKindEnum.CLICK, "Login", "Login", "group", "", IN_PAGE, page: 1);
            int menu = Press(RecordedActionKindEnum.CLICK, "Open Menu", "", "button", "Open Menu", IN_PAGE, page: 2);
            int logout = Press(RecordedActionKindEnum.CLICK, "pgout", "Logout", "group", "", IN_PAGE, page: 2);

            _recording.ScreenChanges.Add(new ScreenChangeDto { ActionIndex = login, NextActionIndex = menu, ChangedShare = 0.9, IsNewScreen = true, Appeared = ["Products", "Name (A to Z)", "Sauce Labs Backpack", "Sauce Labs Bike Light"] });
            _recording.ScreenChanges.Add(new ScreenChangeDto { ActionIndex = menu, NextActionIndex = logout, ChangedShare = 0.3, Appeared = ["All Items", "About", "Logout", "Reset App State"], Gone = ["Products"] });
            _recording.ScreenChanges.Add(new ScreenChangeDto { ActionIndex = logout, ChangedShare = 0.9, IsNewScreen = true, Appeared = ["Accepted usernames are:", "standard_user", "Login"] });
            _recording.ScreenDescriptions.Add(new ScreenDescriptionDto { ActionIndex = 0, Page = "Swag Labs login page" });
            _recording.ScreenDescriptions.Add(new ScreenDescriptionDto { ActionIndex = menu, Page = "The Swag Labs product listing page." });
        }

        // A press as the recorder found it: a box around the click with what OCR read in it, the
        // whole line under it, and what Windows said it was.
        private int Press(RecordedActionKindEnum kind, string read, string line, string role, string name, string[] inside, int page, bool isDouble = false)
        {
            int index = _recording.Actions.Count;
            _recording.Actions.Add(new RecordedActionDto
            {
                Index = index,
                Kind = kind,
                LocationX = 400,
                LocationY = 300 + index,
                LocationEndX = 900,
                LocationEndY = 300,
                ScreenshotIndex = index,
                WaitedMilliseconds = 800,
                Window = RecordedWindowEnum.MAIN_AREA,
                CursorButtonType = CursorButtonTypeEnum.LEFT_BUTTON,
                CursorButtonActionType = isDouble ? CursorButtonActionTypeEnum.DOUBLE_CLICK : CursorButtonActionTypeEnum.SINGLE_CLICK,
            });

            _recording.ClickedElements.Add(new ClickedElementDto { ActionIndex = index, X = 360, Y = 260 + index, Width = 80, Height = 80, Text = read, LineText = line, FoundBy = ElementFoundByEnum.FALLBACK, Image = [2] });
            _recording.PressedElements.Add(new PressedElementDto { ActionIndex = index, Role = role, Name = name, Inside = inside.ToList() });
            _recording.ScreenPages.Add(new ScreenPageDto { ActionIndex = index, Page = page });
            _reviewed.Actions.Add(new ReviewedActionDto { ActionIndex = index, Choice = ReviewChoiceEnum.WAIT_AND_CLICK });

            return index;
        }

        private void Copy()
        {
            Key(RecordedActionKindEnum.KEY_HOLD, "Ctrl");
            Key(RecordedActionKindEnum.KEY_COMBINATION, "C");
            Key(RecordedActionKindEnum.KEY_RELEASE, "Ctrl");
        }

        private void Paste()
        {
            Key(RecordedActionKindEnum.KEY_HOLD, "Ctrl");
            Key(RecordedActionKindEnum.KEY_COMBINATION, "V");
            Key(RecordedActionKindEnum.KEY_RELEASE, "Ctrl");
        }

        private void Key(RecordedActionKindEnum kind, string key)
        {
            int index = _recording.Actions.Count;
            _recording.Actions.Add(new RecordedActionDto { Index = index, Kind = kind, Text = key });
            _reviewed.Actions.Add(new ReviewedActionDto { ActionIndex = index, Choice = ReviewChoiceEnum.KEEP });
        }

        private RecordingScreenDto? ScreenAt(int actionIndex)
        {
            RecordedActionDto? press = _recording.Actions.LastOrDefault(x => x.Index <= actionIndex && x.ScreenshotIndex != null);
            if (press == null)
                return null;

            return new RecordingScreenDto { ActionIndex = press.Index, Image = [1], X = 0, Y = 0, Width = 1900, Height = 1000, Dpi = 120 };
        }

        private RecordingDraft Draft(bool isQaTest = false)
        {
            FlowArea firefox = new FlowArea { Name = "firefox", IsMain = true, Type = FlowAreaTypeEnum.APPLICATION, ProcessName = "firefox", ScalesWith = ScalesWithEnum.DPI };
            Flow flow = new Flow
            {
                Name = "Swag Labs sign in",
                Description = "Sign in to Swag Labs with the standard user by copying the username and password shown on the page, then sign out.",
                IsQaTest = isQaTest,
            };

            FlowScriptSchema schema = new FlowScriptSchema { Flow = flow };
            schema.Areas.Add(firefox);

            return new RecordingDraftBuilder(new FakeScreenshotService()).Build(schema, _recording, _reviewed, ScreenAt, null, null);
        }

        private static List<string> SearchNames(RecordingDraft draft)
        {
            return draft.Items.Where(x => x.Kind == DraftItemKindEnum.SEARCH).Select(x => x.Step.Name).ToList();
        }

        // ================================================================
        // Without a model
        // ================================================================

        [Fact]
        public void Without_a_model_the_steps_are_named_as_Windows_named_them_or_by_the_whole_line_under_the_click()
        {
            SearchNames(Draft()).ShouldBe(
            [
                "Find standard_user",
                "Find Username",
                "Find Username 2",
                "Find secret_sauce",
                "Find Password",
                "Find Login",
                "Find Open Menu",
                "Find Logout",
            ]);
        }

        // The click on Login opened the products page, so the menu and signing out are under it.
        [Fact]
        public void What_was_done_on_the_products_page_sits_under_the_click_that_opened_it()
        {
            FlowScriptSchema schema = Draft().Lay();

            List<string> roots = schema.Steps.Where(x => x.ParentFlowStep == null && x.FlowStepType == FlowStepTypeEnum.SEARCH_IMAGE).OrderBy(x => x.OrderNumber).Select(x => x.Name).ToList();
            roots.ShouldBe(["Find standard_user", "Find Username", "Find Username 2", "Find secret_sauce", "Find Password", "Find Login"]);

            FlowStep login = schema.Steps.Single(x => x.Name == "Find Login");
            FlowStep success = schema.Steps.Single(x => x.ParentFlowStep == login && x.FlowStepType == FlowStepTypeEnum.SUCCESS);
            schema.Steps.Where(x => x.ParentFlowStep == success && x.FlowStepType == FlowStepTypeEnum.SEARCH_IMAGE).OrderBy(x => x.OrderNumber).Select(x => x.Name)
                .ShouldBe(["Find Open Menu", "Find Logout"]);

            string script = new Printer().Write(schema);
            new Scanner().Read(script).Diagnostics.ShouldBeEmpty(script);
        }

        [Fact]
        public void The_outline_lists_every_search_with_what_was_done_after_it()
        {
            RecordingOutline outline = RecordingOutline.Of(Draft(), _recording);

            outline.Searches.Keys.ShouldBe(["S1", "S2", "S3", "S4", "S5", "S6", "S7", "S8"]);
            outline.Text.ShouldContain("S1  Find standard_user\n    screen: the local model sees the page as \"Swag Labs login page\"");
            outline.Text.ShouldContain("    then: double click, hold Ctrl, press C, release Ctrl");
            outline.Text.ShouldContain("    then: drag to a spot, hold Ctrl, press V, release Ctrl");
            outline.Text.ShouldContain("    screen: after the click appeared \"All Items\", \"About\", \"Logout\", \"Reset App State\"");
        }

        // ================================================================
        // With the local model
        // ================================================================

        [Fact]
        [Trait("Category", "Model")]
        public async Task The_local_model_names_most_steps_and_proposes_only_checks_that_were_seen()
        {
            RecordingDraft draft = Draft(isQaTest: true);
            List<string> before = SearchNames(draft);
            RecordingOutline outline = RecordingOutline.Of(draft, _recording);

            RecordingWordsService service = new RecordingWordsService(new LocalModel(), NullLogger<RecordingWordsService>.Instance);
            RecordingWordsResult named = await service.NameAsync(outline, service.RequestOf(outline, string.Empty), true, null, Ct);
            named.Words.ShouldNotBeNull(named.ErrorMessage);

            AppliedWords applied = new AppliedWords();
            RecordingWordsApplier.ApplyNames(draft, outline, named.Words, applied);

            RecordingOutline renamed = RecordingOutline.Of(draft, _recording);
            RecordingWordsResult proposed = await service.ProposeTestAsync(renamed, true, null, Ct);
            proposed.Words.ShouldNotBeNull(proposed.ErrorMessage);
            RecordingWordsApplier.ApplyTest(draft, renamed, proposed.Words, _recording, applied);

            ITestOutputHelper? output = TestContext.Current.TestOutputHelper;
            output?.WriteLine(renamed.Text);
            output?.WriteLine(string.Join("\n", draft.Items.Select(x => $"{x.Kind} {x.Step.Name} | {x.Step.CodeComment.Replace('\n', ' ')} | {x.End?.Message}")));
            output?.WriteLine(string.Join("\n", applied.NotTaken));

            // The plain draft's names are good already, so a model may keep them; what it must
            // write is why each search is there and what its failure means.
            List<DraftItem> searches = draft.Items.Where(x => x.Kind == DraftItemKindEnum.SEARCH && !x.IsProposed).ToList();
            searches.Select(x => x.Step.Name).ShouldAllBe(x => x.StartsWith("Find", StringComparison.Ordinal));
            searches.Count(x => x.Step.CodeComment != $"recorded after a 0.8s wait").ShouldBeGreaterThanOrEqualTo(6);
            searches.Count(x => x.End != null && !x.End.Message.Contains("found nothing within", StringComparison.Ordinal)).ShouldBeGreaterThanOrEqualTo(6);
            SearchNames(draft).Count.ShouldBe(before.Count + draft.Items.Count(x => x.IsProposed));

            List<string> checkWords = draft.Items.Where(x => x.IsProposed).Select(x => x.Step.ConditionText).ToList();
            List<string> appeared = _recording.ScreenChanges.SelectMany(x => x.Appeared).ToList();
            checkWords.ShouldAllBe(x => appeared.Any(y => y.Contains(x, StringComparison.OrdinalIgnoreCase)));

            string script = new Printer().Write(draft.Lay());
            new Scanner().Read(script).Diagnostics.ShouldBeEmpty(script);
        }


        // ================================================================
        // Private types
        // ================================================================

        // Ollama on this machine, as the app talks to it.
        private sealed class LocalModel : IAiClientFactory
        {
            public Task<IChatClient?> CreateAsync(CancellationToken ct = default)
            {
                IChatClient client = new OllamaContextChatClient(new OllamaApiClient(new Uri(OLLAMA), MODEL), 16384);
                return Task.FromResult<IChatClient?>(client);
            }

            public Task<IChatClient?> CreateLocalAsync(CancellationToken ct = default)
            {
                return CreateAsync(ct);
            }
        }
    }
}
