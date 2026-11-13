using System.Drawing;
using Business.Ai.Vision;
using Business.Ai.Vision.Models;
using Business.Recording.Models.Session;
using Core.Enums;
using Core.Helpers;
using Core.Models.Business;
using Core.Models.Dtos;
using Core.Ports;

namespace Business.Recording.Elements
{
    /// <summary>
    /// Per click, drag and gesture: the element under the press, found in the screenshot of the main area
    /// from just before it, and the text on it, read by OCR. The rectangle is what the draft cuts
    /// a template from; the text is what names the step.
    ///
    /// A press outside the main area is not looked at: there is no screenshot of where it landed.
    /// Read text is screen data, so it reaches a cloud model only through the screen data gate.
    ///
    /// A gesture's press is found while the recording is paused to ask about it, and the stop's
    /// finding takes it as it was. One at a time, since the detector reads one image at once.
    /// </summary>
    public sealed class ClickedElementFinder : IClickedElementFinder, IDisposable
    {
        private readonly IElementLocator _elementLocator;
        private readonly IOcrService _ocrService;
        private readonly IScreenshotService _screenshotService;
        private readonly SemaphoreSlim _gate = new SemaphoreSlim(1, 1);

        public ClickedElementFinder(IElementLocator elementLocator, IOcrService ocrService, IScreenshotService screenshotService)
        {
            _elementLocator = elementLocator;
            _ocrService = ocrService;
            _screenshotService = screenshotService;
        }


        // ================================================================
        // Public methods
        // ================================================================

        public async Task<IReadOnlyList<ClickedElementDto>> FindAsync(
            IReadOnlyList<RecordedActionDto> actions,
            Func<int, RecordedPress?> pressOf,
            IReadOnlyDictionary<int, ClickedElementDto> foundByPress,
            Action<int, int> onProgress,
            CancellationToken ct = default)
        {
            List<RecordedActionDto> clicks = actions.Where(x => IsPress(x, pressOf)).ToList();
            List<ClickedElementDto> elements = new List<ClickedElementDto>();

            onProgress(0, clicks.Count);

            await _gate.WaitAsync(ct);
            try
            {
                for (int done = 0; done < clicks.Count; done++)
                {
                    ct.ThrowIfCancellationRequested();

                    RecordedActionDto click = clicks[done];
                    ClickedElementDto? found;
                    if (click.PressIndex != null && foundByPress.TryGetValue(click.PressIndex.Value, out ClickedElementDto? asked))
                    {
                        found = ForAction(asked, click.Index);
                    }
                    else
                    {
                        _elementLocator.Load();
                        found = await FindOneAsync(click, pressOf, ct);
                    }

                    if (found != null)
                        elements.Add(found);

                    onProgress(done + 1, clicks.Count);
                }
            }
            finally
            {
                // Hundreds of megabytes the app has no use for between recordings, loaded here or by a gesture.
                _elementLocator.Unload();
                _gate.Release();
            }

            return elements;
        }

        public async Task<ClickedElementDto?> FindAtAsync(RecordedPress press, int screenX, int screenY, CancellationToken ct = default)
        {
            await _gate.WaitAsync(ct);
            try
            {
                _elementLocator.Load();
                return await FindAtPressAsync(press, screenX, screenY, -1, ct);
            }
            finally
            {
                _gate.Release();
            }
        }

        public void Unload()
        {
            _gate.Wait();
            try
            {
                _elementLocator.Unload();
            }
            finally
            {
                _gate.Release();
            }
        }

        public void Dispose()
        {
            _gate.Dispose();
        }


        // ================================================================
        // Private methods
        // ================================================================

        // A step marked instead of a click too, for a notification that reads the words there.
        private static bool IsPress(RecordedActionDto action, Func<int, RecordedPress?> pressOf)
        {
            bool isPointed = action.Kind == RecordedActionKindEnum.CLICK
                || action.Kind == RecordedActionKindEnum.DRAG
                || action.Kind == RecordedActionKindEnum.CHECK
                || action.Kind == RecordedActionKindEnum.SUBSTITUTE;
            return isPointed && action.ScreenshotIndex != null && pressOf(action.ScreenshotIndex.Value) != null;
        }

        // One screenshot decoded at a time: a recording's worth of them would be hundreds of megabytes.
        private Task<ClickedElementDto?> FindOneAsync(RecordedActionDto action, Func<int, RecordedPress?> pressOf, CancellationToken ct)
        {
            RecordedPress press = pressOf(action.ScreenshotIndex!.Value)!;
            return FindAtPressAsync(press, action.LocationX, action.LocationY, action.Index, ct);
        }

        private async Task<ClickedElementDto?> FindAtPressAsync(RecordedPress press, int screenX, int screenY, int actionIndex, CancellationToken ct)
        {
            RawImage image = _screenshotService.Decode(press.Screenshot);
            Rectangle whole = new Rectangle(0, 0, image.Width, image.Height);

            Point point = new Point(screenX - press.MainAreaBounds.X, screenY - press.MainAreaBounds.Y);
            if (!whole.Contains(point))
                return null;

            LocatedElement element = _elementLocator.Locate(image, point, press.Dpi);
            RawImage crop = RawImageHelper.Crop(image, whole, element.Bounds);
            string text = await ReadTextAsync(crop, press.Dpi, ct);

            return new ClickedElementDto
            {
                ActionIndex = actionIndex,
                X = element.Bounds.X,
                Y = element.Bounds.Y,
                Width = element.Bounds.Width,
                Height = element.Bounds.Height,
                Text = text,
                FoundBy = element.FoundBy,
                Note = element.Note,
                Image = _screenshotService.Encode(crop, ScreenshotFormatEnum.PNG, 100),
            };
        }

        // Found before its action had a number.
        private static ClickedElementDto ForAction(ClickedElementDto found, int actionIndex)
        {
            return new ClickedElementDto
            {
                ActionIndex = actionIndex,
                X = found.X,
                Y = found.Y,
                Width = found.Width,
                Height = found.Height,
                Text = found.Text,
                FoundBy = found.FoundBy,
                Note = found.Note,
                Image = found.Image,
            };
        }

        // On one line, as a name is. An element OCR cannot read is kept, with no text.
        // Faint text on a small button often reads only at twice the size, so that is the second try.
        private async Task<string> ReadTextAsync(RawImage crop, int dpi, CancellationToken ct)
        {
            try
            {
                string read = await _ocrService.ReadAsync(crop, string.Empty, dpi, ct);
                if (string.IsNullOrWhiteSpace(read))
                {
                    RawImage enlarged = RawImageHelper.Enlarge(crop, 2);
                    read = await _ocrService.ReadAsync(enlarged, string.Empty, dpi, ct);
                }

                string[] words = read.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
                return string.Join(' ', words);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                Console.Error.WriteLine($"[ClickedElementFinder] Could not read the text on an element: {ex.Message}");
                return string.Empty;
            }
        }
    }
}
