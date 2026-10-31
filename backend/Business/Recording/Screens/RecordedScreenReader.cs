using Business.Recording.Models.Session;
using Business.ScreenText;
using Business.ScreenText.Models;
using Core.Enums;
using Core.Models.Business;
using Core.Models.Dtos;
using Core.Ports;

namespace Business.Recording.Screens
{
    /// <summary>
    /// Every press's screenshot of the main area read whole by OCR - about 80 ms each - and
    /// compared with the one before it: what the actions between the two made appear and go, and
    /// how much of the text changed, and whether the main area's window took another title - for a
    /// browser, another page. A press's screenshot is from just before it, so what a click did
    /// shows at the next press, and what the last one did on the screen the recording stopped on.
    ///
    /// One screenshot decoded at a time, as the clicked elements are. The language is the user's,
    /// as it is for the text under a click.
    /// </summary>
    public sealed class RecordedScreenReader : IRecordedScreenReader
    {
        private const int SHARE_DECIMALS = 2;

        private readonly IOcrService _ocrService;
        private readonly IScreenshotService _screenshotService;

        public RecordedScreenReader(IOcrService ocrService, IScreenshotService screenshotService)
        {
            _ocrService = ocrService;
            _screenshotService = screenshotService;
        }


        // ================================================================
        // Public methods
        // ================================================================

        public async Task<RecordedScreens> ReadAsync(
            IReadOnlyList<RecordedActionDto> actions,
            Func<int, RecordedPress?> pressOf,
            RawImage? lastScreen,
            Action<int, int> onProgress,
            CancellationToken ct = default)
        {
            List<RecordedActionDto> pressed = actions.Where(x => x.ScreenshotIndex != null && pressOf(x.ScreenshotIndex.Value) != null).ToList();
            int total = pressed.Count;
            if (lastScreen != null)
                total++;

            RecordedScreens screens = new RecordedScreens();
            HashSet<string> read = new HashSet<string>(StringComparer.Ordinal);

            onProgress(0, total);

            List<ScreenLine>? before = null;
            for (int done = 0; done < pressed.Count; done++)
            {
                ct.ThrowIfCancellationRequested();

                RecordedPress press = pressOf(pressed[done].ScreenshotIndex!.Value)!;
                List<ScreenLine> lines = await ReadLinesAsync(_screenshotService.Decode(press.Screenshot), screens, read, ct);

                if (before != null)
                    screens.Changes.Add(ChangeOf(pressed[done - 1], pressed[done], ScreenTextComparer.Compare(before, lines)));

                before = lines;
                onProgress(done + 1, total);
            }

            if (lastScreen != null)
            {
                ct.ThrowIfCancellationRequested();

                List<ScreenLine> lines = await ReadLinesAsync(lastScreen, screens, read, ct);
                if (before != null)
                    screens.Changes.Add(ChangeOf(pressed[^1], null, ScreenTextComparer.Compare(before, lines)));

                onProgress(total, total);
            }

            return screens;
        }


        // ================================================================
        // Private methods
        // ================================================================

        private async Task<List<ScreenLine>> ReadLinesAsync(RawImage image, RecordedScreens screens, HashSet<string> read, CancellationToken ct)
        {
            IReadOnlyList<OcrWord> words = await _ocrService.ReadWordsAsync(image, string.Empty, ct);
            List<ScreenLine> lines = ScreenLineBuilder.Build(words);

            foreach (ScreenLine line in lines.Where(x => read.Add(x.Text)))
                screens.Lines.Add(line.Text);

            return lines;
        }

        // To the next press, or to the screen the recording stopped on, which has no title.
        private static ScreenChangeDto ChangeOf(RecordedActionDto from, RecordedActionDto? to, ScreenTextChange change)
        {
            string? fromTitle = TitleOf(from);
            string? toTitle = null;
            if (to != null)
                toTitle = TitleOf(to);

            return new ScreenChangeDto
            {
                ActionIndex = from.Index,
                NextActionIndex = to?.Index,
                Appeared = change.Appeared.Select(x => x.Text).ToList(),
                Gone = change.Gone.Select(x => x.Text).ToList(),
                ChangedShare = Math.Round(change.ChangedShare, SHARE_DECIMALS),
                IsNewScreen = change.IsNewScreen,
                IsNewTitle = fromTitle != null && toTitle != null && fromTitle != toTitle,
                Title = toTitle,
            };
        }

        // The title of the window pressed on, when it is the main area's: a press on another window,
        // the taskbar, says nothing of the page.
        private static string? TitleOf(RecordedActionDto action)
        {
            if (action.Window != RecordedWindowEnum.MAIN_AREA || string.IsNullOrWhiteSpace(action.WindowTitle))
                return null;

            return action.WindowTitle.Trim();
        }
    }
}
