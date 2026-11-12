using System.Drawing;
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
    /// Every press's screenshot of the main area read whole by OCR - about a second each - and
    /// compared with the one before it: what the actions between the two made appear and go, and
    /// how much of the text changed, and whether the main area's window took another title - for a
    /// browser, another page. A press's screenshot is from just before it, so what a click did
    /// shows at the next press, and what the last one did on the screen the recording stopped on.
    ///
    /// Which page each press was on comes out of the same comparison: a new page is compared with
    /// every page seen before it, so a dialog closed or a menu left is the page it was opened from,
    /// come back to, and not one more. And the line under each click is read here, whole, where
    /// the text read on the element is cut by its rectangle.
    ///
    /// One screenshot decoded at a time, as the clicked elements are. Read with the default
    /// reader, Latin, as the text under a click is.
    /// </summary>
    public sealed class RecordedScreenReader : IRecordedScreenReader
    {
        private const int SHARE_DECIMALS = 2;

        private static readonly RecordedActionKindEnum[] POINTED = [RecordedActionKindEnum.CLICK, RecordedActionKindEnum.DRAG, RecordedActionKindEnum.CHECK];

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
            IReadOnlyList<ClickedElementDto> elements,
            RecordingScreenshot? lastScreen,
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

            List<SeenPage> seen = new List<SeenPage>();
            SeenPage? page = null;

            List<ScreenLine>? before = null;
            for (int done = 0; done < pressed.Count; done++)
            {
                ct.ThrowIfCancellationRequested();

                RecordedActionDto action = pressed[done];
                RecordedPress press = pressOf(action.ScreenshotIndex!.Value)!;
                List<ScreenLine> lines = await ReadLinesAsync(_screenshotService.Decode(press.Screenshot), press.Dpi, screens, read, ct);

                string under = LineUnder(action, press, lines, elements);
                if (under.Length > 0)
                    screens.LineTexts[action.Index] = under;

                bool isNewPage = false;
                if (before != null)
                {
                    ScreenChangeDto change = ChangeOf(pressed[done - 1], action, ScreenTextComparer.Compare(before, lines));
                    screens.Changes.Add(change);
                    isNewPage = change.IsNewScreen || change.IsNewTitle;
                }

                page = PageOf(seen, page, lines, TitleOf(action), isNewPage);
                screens.Pages.Add(new ScreenPageDto { ActionIndex = action.Index, Page = page.Number });

                before = lines;
                onProgress(done + 1, total);
            }

            if (lastScreen != null)
            {
                ct.ThrowIfCancellationRequested();

                List<ScreenLine> lines = await ReadLinesAsync(lastScreen.Image, lastScreen.Target.Dpi, screens, read, ct);
                if (before != null)
                    screens.Changes.Add(ChangeOf(pressed[^1], null, ScreenTextComparer.Compare(before, lines)));

                onProgress(total, total);
            }

            return screens;
        }


        // ================================================================
        // Private methods
        // ================================================================

        private async Task<List<ScreenLine>> ReadLinesAsync(RawImage image, int dpi, RecordedScreens screens, HashSet<string> read, CancellationToken ct)
        {
            IReadOnlyList<OcrWord> words = await _ocrService.ReadWordsAsync(image, string.Empty, dpi, ct);
            List<ScreenLine> lines = ScreenLineBuilder.Build(words);

            foreach (ScreenLine line in lines.Where(x => read.Add(x.Text)))
                screens.Lines.Add(line.Text);

            return lines;
        }

        // The page it was on before, unless a new one began: then one seen earlier that reads the
        // same under the same title, the latest such, or one more.
        private static SeenPage PageOf(List<SeenPage> seen, SeenPage? current, List<ScreenLine> lines, string? title, bool isNewPage)
        {
            if (current != null && !isNewPage)
                return current;

            SeenPage? back = seen.LastOrDefault(x => x != current && IsSamePage(x, lines, title));
            if (back != null)
                return back;

            SeenPage page = new SeenPage(seen.Count + 1, lines, title);
            seen.Add(page);

            return page;
        }

        private static bool IsSamePage(SeenPage page, List<ScreenLine> lines, string? title)
        {
            if (page.Title != null && title != null && page.Title != title)
                return false;

            return !ScreenTextComparer.Compare(page.Lines, lines).IsNewScreen;
        }

        // The smallest line the click is on, else the nearest one its element's rectangle touches:
        // a text field clicked beside its placeholder.
        private static string LineUnder(RecordedActionDto action, RecordedPress press, List<ScreenLine> lines, IReadOnlyList<ClickedElementDto> elements)
        {
            if (!POINTED.Contains(action.Kind))
                return string.Empty;

            Point click = new Point(action.LocationX - press.MainAreaBounds.X, action.LocationY - press.MainAreaBounds.Y);

            ScreenLine? under = lines.Where(x => x.Bounds.Contains(click)).MinBy(x => x.Bounds.Width * x.Bounds.Height);
            if (under != null)
                return under.Text;

            ClickedElementDto? element = elements.FirstOrDefault(x => x.ActionIndex == action.Index);
            if (element == null)
                return string.Empty;

            Rectangle rectangle = new Rectangle(element.X, element.Y, element.Width, element.Height);
            ScreenLine? touched = lines.Where(x => x.Bounds.IntersectsWith(rectangle)).MinBy(x => DistanceTo(x.Bounds, click));

            return touched?.Text ?? string.Empty;
        }

        private static double DistanceTo(Rectangle bounds, Point point)
        {
            double x = bounds.Left + bounds.Width / 2d - point.X;
            double y = bounds.Top + bounds.Height / 2d - point.Y;

            return Math.Sqrt(x * x + y * y);
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


        // ================================================================
        // Private types
        // ================================================================

        // A page as first seen: what was read on it, and its window's title when pressed in the main area.
        private sealed record SeenPage(int Number, List<ScreenLine> Lines, string? Title);
    }
}
