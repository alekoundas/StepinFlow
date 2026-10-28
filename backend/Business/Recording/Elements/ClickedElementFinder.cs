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
    /// Per click and drag: the element under the press, found in the screenshot of the main area
    /// from just before it, and the text on it, read by OCR. The rectangle is what the draft cuts
    /// a template from; the text is what names the step.
    ///
    /// A press outside the main area is not looked at: there is no screenshot of where it landed.
    /// Read text is screen data, so it reaches a cloud model only through the screen data gate.
    /// </summary>
    public sealed class ClickedElementFinder : IClickedElementFinder
    {
        private readonly IElementLocator _elementLocator;
        private readonly IOcrService _ocrService;
        private readonly IScreenshotService _screenshotService;

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
            Action<int, int> onProgress,
            CancellationToken ct = default)
        {
            List<RecordedActionDto> clicks = actions.Where(x => IsPress(x, pressOf)).ToList();
            List<ClickedElementDto> elements = new List<ClickedElementDto>();

            onProgress(0, clicks.Count);
            if (clicks.Count == 0)
                return elements;

            _elementLocator.Load();

            try
            {
                for (int done = 0; done < clicks.Count; done++)
                {
                    ct.ThrowIfCancellationRequested();

                    ClickedElementDto? found = await FindOneAsync(clicks[done], pressOf, ct);
                    if (found != null)
                        elements.Add(found);

                    onProgress(done + 1, clicks.Count);
                }
            }
            finally
            {
                _elementLocator.Unload();
            }

            return elements;
        }


        // ================================================================
        // Private methods
        // ================================================================

        private static bool IsPress(RecordedActionDto action, Func<int, RecordedPress?> pressOf)
        {
            bool isPointed = action.Kind == RecordedActionKindEnum.CLICK || action.Kind == RecordedActionKindEnum.DRAG || action.Kind == RecordedActionKindEnum.CHECK;
            return isPointed && action.ScreenshotIndex != null && pressOf(action.ScreenshotIndex.Value) != null;
        }

        // One screenshot decoded at a time: a recording's worth of them would be hundreds of megabytes.
        private async Task<ClickedElementDto?> FindOneAsync(RecordedActionDto action, Func<int, RecordedPress?> pressOf, CancellationToken ct)
        {
            RecordedPress press = pressOf(action.ScreenshotIndex!.Value)!;
            RawImage image = _screenshotService.Decode(press.Screenshot);
            Rectangle whole = new Rectangle(0, 0, image.Width, image.Height);

            Point point = new Point(action.LocationX - press.MainAreaBounds.X, action.LocationY - press.MainAreaBounds.Y);
            if (!whole.Contains(point))
                return null;

            LocatedElement element = _elementLocator.Locate(image, point, press.Dpi);
            RawImage crop = RawImageHelper.Crop(image, whole, element.Bounds);
            string text = await ReadTextAsync(crop, ct);

            return new ClickedElementDto
            {
                ActionIndex = action.Index,
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

        // On one line, as a name is. An element OCR cannot read is kept, with no text.
        // Faint text on a small button often reads only at twice the size, so that is the second try.
        private async Task<string> ReadTextAsync(RawImage crop, CancellationToken ct)
        {
            try
            {
                string read = await _ocrService.ReadAsync(crop, string.Empty, ct);
                if (string.IsNullOrWhiteSpace(read))
                {
                    RawImage enlarged = RawImageHelper.Enlarge(crop, 2);
                    read = await _ocrService.ReadAsync(enlarged, string.Empty, ct);
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
