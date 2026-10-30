using Business.Recording.Models.Session;
using Business.ScreenText;
using Business.ScreenText.Models;
using Core.Models.Business;
using Core.Models.Dtos;
using Core.Ports;

namespace Business.Recording.Screens
{
    /// <summary>
    /// Every press's screenshot of the main area read whole by OCR - about 80 ms each - and
    /// compared with the one before it: what the actions between the two made appear and go, and
    /// how much of the text changed. A press's screenshot is from just before it, so what a click
    /// did shows at the next press.
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

        public async Task<IReadOnlyList<ScreenChangeDto>> ReadAsync(
            IReadOnlyList<RecordedActionDto> actions,
            Func<int, RecordedPress?> pressOf,
            Action<int, int> onProgress,
            CancellationToken ct = default)
        {
            List<RecordedActionDto> pressed = actions.Where(x => x.ScreenshotIndex != null && pressOf(x.ScreenshotIndex.Value) != null).ToList();
            List<ScreenChangeDto> changes = new List<ScreenChangeDto>();

            onProgress(0, pressed.Count);

            List<ScreenLine>? before = null;
            for (int done = 0; done < pressed.Count; done++)
            {
                ct.ThrowIfCancellationRequested();

                RecordedPress press = pressOf(pressed[done].ScreenshotIndex!.Value)!;
                RawImage image = _screenshotService.Decode(press.Screenshot);
                IReadOnlyList<OcrWord> words = await _ocrService.ReadWordsAsync(image, string.Empty, ct);
                List<ScreenLine> lines = ScreenLineBuilder.Build(words);

                if (before != null)
                    changes.Add(ChangeOf(pressed[done - 1], pressed[done], ScreenTextComparer.Compare(before, lines)));

                before = lines;
                onProgress(done + 1, pressed.Count);
            }

            return changes;
        }


        // ================================================================
        // Private methods
        // ================================================================

        private static ScreenChangeDto ChangeOf(RecordedActionDto from, RecordedActionDto to, ScreenTextChange change)
        {
            return new ScreenChangeDto
            {
                ActionIndex = from.Index,
                NextActionIndex = to.Index,
                Appeared = change.Appeared.Select(x => x.Text).ToList(),
                Gone = change.Gone.Select(x => x.Text).ToList(),
                ChangedShare = Math.Round(change.ChangedShare, SHARE_DECIMALS),
                IsNewScreen = change.IsNewScreen,
            };
        }
    }
}
