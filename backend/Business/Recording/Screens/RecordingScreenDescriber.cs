using Business.Ai.Helpers;
using Business.Ai.Providers;
using Core.Enums;
using Core.Helpers;
using Core.Models.Business;
using Core.Models.Dtos;
using Core.Ports;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;

namespace Business.Recording.Screens
{
    /// <summary>
    /// The local model looking at the screen, as a reviewed recording goes to a model: one sentence
    /// for the first screen and every one where a new page began - by its words or by the window's
    /// title - and a few words for every kept click that neither OCR nor Windows named, an icon in a
    /// game mostly. From the picture, never from the OCR text: given only the text, the model made up
    /// what a screen was for.
    ///
    /// Always the local model, even when a cloud one answers the draft: the screenshots and cutouts
    /// never leave the machine, and only what it says goes on, through the screen data gate. About two
    /// seconds a look on a graphics card, so the looks are capped; a look that fails is passed over.
    /// </summary>
    public sealed class RecordingScreenDescriber : IRecordingScreenDescriber
    {
        private const string VISION = "vision";

        private const int MAXIMUM_PAGES = 8;
        private const int MAXIMUM_ELEMENTS = 12;

        // A sixteen pixel icon is too little for a model to see, so a cutout is enlarged to this at least.
        private const int MINIMUM_CUTOUT_EDGE = 64;
        private const int MAXIMUM_ENLARGEMENT = 4;

        // What the assistant gives a model of a screenshot too.
        private const int SCREENSHOT_EDGE = 1200;
        private const int SCREENSHOT_QUALITY = 80;

        private const int ELEMENT_TOKENS = 30;
        private const int PAGE_TOKENS = 100;
        private const int ELEMENT_LENGTH = 60;
        private const int PAGE_LENGTH = 200;

        private static readonly RecordedActionKindEnum[] POINTED = [RecordedActionKindEnum.CLICK, RecordedActionKindEnum.DRAG, RecordedActionKindEnum.CHECK];

        private readonly IAiClientFactory _clientFactory;
        private readonly IAiProviderService _providerService;
        private readonly IOllamaService _ollamaService;
        private readonly IScreenshotService _screenshotService;
        private readonly IOpenCvService _openCvService;
        private readonly ILogger<RecordingScreenDescriber> _logger;

        public RecordingScreenDescriber(
            IAiClientFactory clientFactory,
            IAiProviderService providerService,
            IOllamaService ollamaService,
            IScreenshotService screenshotService,
            IOpenCvService openCvService,
            ILogger<RecordingScreenDescriber> logger)
        {
            _clientFactory = clientFactory;
            _providerService = providerService;
            _ollamaService = ollamaService;
            _screenshotService = screenshotService;
            _openCvService = openCvService;
            _logger = logger;
        }


        // ================================================================
        // Public methods
        // ================================================================

        public async Task<IReadOnlyList<ScreenDescriptionDto>> DescribeAsync(
            RecordingResultDto recording,
            ReviewedRecordingDto reviewed,
            Func<int, RecordingScreenDto?> screenAt,
            Action<int, int> onProgress,
            CancellationToken ct = default)
        {
            List<Look> looks = LooksAt(recording, reviewed, screenAt);
            if (looks.Count == 0 || !await CanSeeAsync(ct))
            {
                onProgress(0, 0);
                return [];
            }

            using IChatClient? client = await _clientFactory.CreateLocalAsync(ct);
            if (client == null)
            {
                onProgress(0, 0);
                return [];
            }

            Dictionary<int, ScreenDescriptionDto> seen = new Dictionary<int, ScreenDescriptionDto>();
            onProgress(0, looks.Count);

            for (int done = 0; done < looks.Count; done++)
            {
                ct.ThrowIfCancellationRequested();

                Look look = looks[done];
                string? answer = await AskAsync(client, look, ct);
                if (answer != null)
                {
                    ScreenDescriptionDto described = seen.GetValueOrDefault(look.ActionIndex) ?? new ScreenDescriptionDto { ActionIndex = look.ActionIndex };
                    if (look.IsPage)
                        described.Page = answer;
                    else
                        described.Element = answer;

                    seen[look.ActionIndex] = described;
                }

                onProgress(done + 1, looks.Count);
            }

            return seen.Values.OrderBy(x => x.ActionIndex).ToList();
        }


        // ================================================================
        // Private methods
        // ================================================================

        // Kept presses only, in the order they happened: a page where one began, then the click.
        private static List<Look> LooksAt(RecordingResultDto recording, ReviewedRecordingDto reviewed, Func<int, RecordingScreenDto?> screenAt)
        {
            HashSet<int> kept = reviewed.Actions.Where(x => x.Choice != ReviewChoiceEnum.LEAVE_OUT).Select(x => x.ActionIndex).ToHashSet();
            List<RecordedActionDto> pressed = recording.Actions.Where(x => kept.Contains(x.Index) && x.ScreenshotIndex != null).ToList();

            List<Look> pages = new List<Look>();
            List<Look> elements = new List<Look>();

            for (int i = 0; i < pressed.Count; i++)
            {
                RecordedActionDto action = pressed[i];

                RecordingScreenDto? screen = screenAt(action.Index);
                if (IsPageStart(recording, action, i == 0) && screen != null && screen.ActionIndex == action.Index)
                    pages.Add(new Look(action.Index, true, screen.Image));

                byte[]? cutout = WordlessCutoutOf(recording, action);
                if (cutout != null)
                    elements.Add(new Look(action.Index, false, cutout));
            }

            return pages.Take(MAXIMUM_PAGES)
                .Concat(elements.Take(MAXIMUM_ELEMENTS))
                .OrderBy(x => x.ActionIndex)
                .ThenByDescending(x => x.IsPage)
                .ToList();
        }

        private static bool IsPageStart(RecordingResultDto recording, RecordedActionDto action, bool isFirst)
        {
            if (isFirst)
                return true;

            ScreenChangeDto? arrived = recording.ScreenChanges.FirstOrDefault(x => x.NextActionIndex == action.Index);
            return arrived != null && (arrived.IsNewScreen || arrived.IsNewTitle);
        }

        // A click OCR read nothing on and Windows had no name for.
        private static byte[]? WordlessCutoutOf(RecordingResultDto recording, RecordedActionDto action)
        {
            if (!POINTED.Contains(action.Kind))
                return null;

            ClickedElementDto? element = recording.ClickedElements.FirstOrDefault(x => x.ActionIndex == action.Index);
            if (element == null || element.Image.Length == 0 || element.Text.Trim().Length > 0)
                return null;

            bool isNamed = recording.PressedElements.Any(x => x.ActionIndex == action.Index && x.Name.Trim().Length > 0);
            if (isNamed)
                return null;

            return element.Image;
        }

        private async Task<bool> CanSeeAsync(CancellationToken ct)
        {
            string model = await _providerService.GetOllamaModelAsync(ct);
            if (string.IsNullOrWhiteSpace(model))
                return false;

            string baseUrl = await _providerService.GetOllamaUrlAsync(ct);
            AiModelDto described = await _ollamaService.DescribeAsync(baseUrl, model, ct);

            return described.Supports(VISION);
        }

        private async Task<string?> AskAsync(IChatClient client, Look look, CancellationToken ct)
        {
            try
            {
                string prompt = AiPromptHelper.NameWordlessElement;
                int tokens = ELEMENT_TOKENS;
                DataContent picture;

                if (look.IsPage)
                {
                    prompt = AiPromptHelper.DescribePage;
                    tokens = PAGE_TOKENS;
                    picture = new DataContent(_openCvService.Downscale(look.Image, SCREENSHOT_EDGE, SCREENSHOT_QUALITY), "image/jpeg");
                }
                else
                {
                    picture = new DataContent(Enlarged(look.Image), "image/png");
                }

                ChatMessage message = new ChatMessage(ChatRole.User, [new TextContent(prompt), picture]);
                ChatOptions options = new ChatOptions { Temperature = 0, MaxOutputTokens = tokens };

                ChatResponse response = await client.GetResponseAsync([message], options, ct);
                return Cleaned(response.Text, look.IsPage);
            }
            catch (Exception ex) when (!ct.IsCancellationRequested)
            {
                _logger.LogWarning(ex, "The local model could not look at the press of action {ActionIndex}.", look.ActionIndex);
                return null;
            }
        }

        private byte[] Enlarged(byte[] cutout)
        {
            RawImage image = _screenshotService.Decode(cutout);
            int shorter = Math.Min(image.Width, image.Height);
            if (shorter <= 0 || shorter >= MINIMUM_CUTOUT_EDGE)
                return cutout;

            int factor = Math.Min(MAXIMUM_ENLARGEMENT, (MINIMUM_CUTOUT_EDGE + shorter - 1) / shorter);
            return _screenshotService.Encode(RawImageHelper.Enlarge(image, factor), ScreenshotFormatEnum.PNG, 100);
        }

        // Its first line, without the quotes and the verb a small model adds anyway. Null for nothing.
        private static string? Cleaned(string answer, bool isPage)
        {
            string line = answer.Split('\n').Select(x => x.Trim()).FirstOrDefault(x => x.Length > 0) ?? string.Empty;
            line = line.Trim('"', '\'', '“', '”', '`', ' ');

            if (!isPage)
            {
                foreach (string verb in new[] { "click on ", "click the ", "click " })
                {
                    if (line.StartsWith(verb, StringComparison.OrdinalIgnoreCase))
                        line = line[verb.Length..];
                }

                line = line.TrimEnd('.', ' ');
            }

            int length = isPage ? PAGE_LENGTH : ELEMENT_LENGTH;
            if (line.Length > length)
                line = line[..length].TrimEnd() + "…";

            if (line.Length == 0)
                return null;

            return line;
        }


        // ================================================================
        // Private types
        // ================================================================

        private sealed record Look(int ActionIndex, bool IsPage, byte[] Image);
    }
}
