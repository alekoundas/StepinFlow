using System.Drawing;
using Business.Ai.Helpers;
using Business.Recording.Screens;
using Business.Tests.Fakes;
using Core.Enums;
using Core.Models.Business;
using Core.Models.Dtos;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging.Abstractions;
using static Business.Tests.TestToken;

namespace Business.Tests.Recording
{
    /// <summary>
    /// The local model looking at the screens worth a look: the first, every one where a new page
    /// began, and every kept click nothing else named.
    /// </summary>
    public sealed class RecordingScreenDescriberTests : IDisposable
    {
        private const string PAGE = "The research screen of a fantasy game.";

        private readonly FakeAiClientFactory _models = new FakeAiClientFactory();
        private readonly FakeOllamaService _ollama = new FakeOllamaService();
        private readonly FakeScreenshotService _screenshots = new FakeScreenshotService();
        private readonly FakeOpenCvService _openCv = new FakeOpenCvService();
        private readonly RecordingScreenDescriber _describer;
        private readonly RecordingResultDto _recording = new RecordingResultDto();
        private readonly ReviewedRecordingDto _reviewed = new ReviewedRecordingDto();
        private readonly FakeChatClient _local;
        private readonly List<(int Done, int Total)> _progress = [];

        public RecordingScreenDescriberTests()
        {
            _describer = new RecordingScreenDescriber(_models, new FakeAiProviderService(), _ollama, _screenshots, _openCv, NullLogger<RecordingScreenDescriber>.Instance);
            _ollama.Capabilities.Add("vision");

            // A small model adds the verb and the full stop it was told not to.
            _local = new FakeChatClient(messages => PromptOf(messages) == AiPromptHelper.DescribePage ? PAGE : "Click settings gear.");
            _models.LocalClient = _local;
        }

        public void Dispose()
        {
            _local.Dispose();
        }

        private static string PromptOf(IReadOnlyList<ChatMessage> messages)
        {
            return messages.Single().Contents.OfType<TextContent>().Single().Text;
        }

        private RecordedActionDto Click(int index, string text, ReviewChoiceEnum choice = ReviewChoiceEnum.WAIT_AND_CLICK)
        {
            RecordedActionDto action = new RecordedActionDto { Index = index, Kind = RecordedActionKindEnum.CLICK, ScreenshotIndex = index };
            _recording.Actions.Add(action);
            _recording.ClickedElements.Add(new ClickedElementDto { ActionIndex = index, Width = 40, Height = 20, Text = text, Image = [2] });
            _reviewed.Actions.Add(new ReviewedActionDto { ActionIndex = index, Choice = choice });

            return action;
        }

        private static RecordingScreenDto ScreenAt(int actionIndex)
        {
            return new RecordingScreenDto { ActionIndex = actionIndex, Image = [1], Width = 800, Height = 600, Dpi = 96 };
        }

        private Task<IReadOnlyList<ScreenDescriptionDto>> DescribeAsync()
        {
            return _describer.DescribeAsync(_recording, _reviewed, ScreenAt, (done, total) => _progress.Add((done, total)), Ct);
        }

        private int Asked(string prompt)
        {
            return _local.Requests.Count(x => PromptOf(x) == prompt);
        }

        [Fact]
        public async Task The_first_screen_and_every_new_page_are_described_from_their_screenshots()
        {
            Click(0, "Login");
            Click(1, "Add to cart");
            Click(2, "Checkout");
            _recording.ScreenChanges.Add(new ScreenChangeDto { ActionIndex = 0, NextActionIndex = 1, IsNewScreen = true });
            _recording.ScreenChanges.Add(new ScreenChangeDto { ActionIndex = 1, NextActionIndex = 2 });

            IReadOnlyList<ScreenDescriptionDto> seen = await DescribeAsync();

            seen.Select(x => (x.ActionIndex, x.Page, x.Element)).ShouldBe([(0, PAGE, null), (1, PAGE, null)]);
            _openCv.DownscaledTo.ShouldBe([1200, 1200]);
            _local.Requests.ShouldAllBe(x => x.Single().Contents.OfType<DataContent>().Single().MediaType == "image/jpeg");
            _progress.ShouldBe([(0, 2), (1, 2), (2, 2)]);
        }

        // A single-page application keeps its words and changes its title.
        [Fact]
        public async Task A_new_title_is_a_new_page_too()
        {
            Click(0, "Login");
            Click(1, "Checkout");
            _recording.ScreenChanges.Add(new ScreenChangeDto { ActionIndex = 0, NextActionIndex = 1, IsNewTitle = true });

            IReadOnlyList<ScreenDescriptionDto> seen = await DescribeAsync();

            seen.Select(x => x.ActionIndex).ShouldBe([0, 1]);
        }

        [Fact]
        public async Task Only_a_click_nothing_named_is_named_from_its_cutout()
        {
            Click(0, "Login");
            Click(1, "");
            Click(2, "");
            _recording.PressedElements.Add(new PressedElementDto { ActionIndex = 2, Role = "link", Name = "Shopping cart" });

            IReadOnlyList<ScreenDescriptionDto> seen = await DescribeAsync();

            seen.Single(x => x.ActionIndex == 1).Element.ShouldBe("settings gear");
            seen.ShouldNotContain(x => x.ActionIndex == 2);
            Asked(AiPromptHelper.NameWordlessElement).ShouldBe(1);
        }

        // Sixteen pixels is too little for a model to see.
        [Fact]
        public async Task A_tiny_cutout_is_enlarged_before_it_is_looked_at()
        {
            Click(0, "");
            _screenshots.DecodedSize = new Size(20, 16);

            await DescribeAsync();

            RawImage sent = _screenshots.Encoded.ShouldHaveSingleItem();
            (sent.Width, sent.Height).ShouldBe((80, 64));
        }

        [Fact]
        public async Task A_click_left_out_in_Review_is_not_looked_at()
        {
            Click(0, "Login");
            Click(1, "", ReviewChoiceEnum.LEAVE_OUT);

            IReadOnlyList<ScreenDescriptionDto> seen = await DescribeAsync();

            seen.ShouldHaveSingleItem().ActionIndex.ShouldBe(0);
            Asked(AiPromptHelper.NameWordlessElement).ShouldBe(0);
        }

        [Fact]
        public async Task A_local_model_that_cannot_take_a_picture_is_not_asked()
        {
            _ollama.Capabilities.Clear();
            Click(0, "");

            IReadOnlyList<ScreenDescriptionDto> seen = await DescribeAsync();

            seen.ShouldBeEmpty();
            _local.Requests.ShouldBeEmpty();
            _progress.ShouldBe([(0, 0)]);
        }

        [Fact]
        public async Task A_look_that_fails_is_passed_over()
        {
            Click(0, "Login");
            Click(1, "");
            FakeChatClient failing = new FakeChatClient(messages => PromptOf(messages) == AiPromptHelper.DescribePage ? throw new HttpRequestException("Ollama stopped") : "cart icon");
            _models.LocalClient = failing;

            IReadOnlyList<ScreenDescriptionDto> seen = await DescribeAsync();

            ScreenDescriptionDto described = seen.Single();
            (described.ActionIndex, described.Page, described.Element).ShouldBe((1, null, "cart icon"));
        }
    }
}
