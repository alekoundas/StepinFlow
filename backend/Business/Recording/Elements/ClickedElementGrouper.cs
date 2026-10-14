using System.Drawing;
using Business.Recording.Models.Session;
using Core.Enums;
using Core.Helpers;
using Core.Models.Business;
using Core.Models.Business.OpenCV;
using Core.Models.Dtos;
using Core.Ports;

namespace Business.Recording.Elements
{
    /// <summary>
    /// Which clicks hit the same picture - "these four clicks were the download icon on four rows" -
    /// which cannot be read off a list of coordinates, and which the draft turns into one template
    /// found in every place rather than four.
    ///
    /// It compares the clicked elements, never the crops around the clicks: the middle of a crop
    /// around a small icon is mostly what surrounds it, so the same icon on two rows does not match
    /// and two toolbar buttons side by side do.
    ///
    /// It is deliberately shy. A group invented here becomes a loop that quietly clicks the wrong
    /// thing, where a group missed only means a draft with more steps than it needed. So only the
    /// elements the model found are compared - a box around the click carries background, as a crop
    /// does - and two whose read text differs never group, however alike they look: "Save" and "Send".
    /// </summary>
    public sealed class ClickedElementGrouper : IClickedElementGrouper
    {
        // Far enough apart that it is another place rather than an imprecise hand.
        private const int MOVED_PIXELS = 12;

        // Enough for a box the model drew a few pixels off the other, at 96 dpi.
        private const int AROUND_PIXELS = 6;

        private readonly IOpenCvService _openCvService;
        private readonly IScreenshotService _screenshotService;

        public ClickedElementGrouper(IOpenCvService openCvService, IScreenshotService screenshotService)
        {
            _openCvService = openCvService;
            _screenshotService = screenshotService;
        }


        // ================================================================
        // Public methods
        // ================================================================

        public IReadOnlyList<ClickGroupDto> Group(
            IReadOnlyList<ClickedElementDto> elements,
            IReadOnlyList<RecordedActionDto> actions,
            Func<int, RecordedPress?> pressOf,
            Action<int, int> onProgress,
            CancellationToken ct = default)
        {
            List<ClickedElementDto> found = elements.Where(x => x.FoundBy == ElementFoundByEnum.MODEL).ToList();
            List<Candidate> candidates = new List<Candidate>();

            onProgress(0, found.Count);

            // Most of the time goes here, decoding each press's screenshot once.
            for (int done = 0; done < found.Count; done++)
            {
                ct.ThrowIfCancellationRequested();

                Candidate? candidate = Prepare(found[done], actions, pressOf);
                if (candidate != null)
                    candidates.Add(candidate);

                onProgress(done + 1, found.Count);
            }

            List<ClickGroupDto> groups = new List<ClickGroupDto>();

            foreach (IGrouping<string, Candidate> sameText in candidates.GroupBy(x => x.Text))
            {
                List<Candidate> members = sameText.ToList();
                List<ElementPicture> pictures = members.Select(x => x.Picture).ToList();

                foreach (IReadOnlyList<int> group in _openCvService.GroupSameElements(pictures))
                {
                    List<ClickedElementDto> grouped = group.Select(x => members[x].Element).ToList();

                    groups.Add(new ClickGroupDto
                    {
                        ActionIndexes = grouped.Select(x => x.ActionIndex).Order().ToList(),
                        IsMoving = Moved(grouped),
                    });
                }
            }

            return groups.OrderBy(x => x.ActionIndexes[0]).ToList();
        }


        // ================================================================
        // Private methods
        // ================================================================

        // The element, and its rectangle widened by a few pixels from the same screenshot.
        private Candidate? Prepare(ClickedElementDto element, IReadOnlyList<RecordedActionDto> actions, Func<int, RecordedPress?> pressOf)
        {
            RecordedActionDto? action = actions.FirstOrDefault(x => x.Index == element.ActionIndex);
            if (action?.ScreenshotIndex == null)
                return null;

            RecordedPress? press = pressOf(action.ScreenshotIndex.Value);
            if (press == null)
                return null;

            RawImage screenshot = _screenshotService.Decode(press.Screenshot);
            Rectangle whole = new Rectangle(0, 0, screenshot.Width, screenshot.Height);

            int margin = AROUND_PIXELS * press.Dpi / 96;
            Rectangle widened = new Rectangle(element.X, element.Y, element.Width, element.Height);
            widened.Inflate(margin, margin);

            RawImage around = RawImageHelper.Crop(screenshot, whole, widened);
            if (around.IsEmpty)
                return null;

            ElementPicture picture = new ElementPicture
            {
                Element = element.Image,
                Around = _screenshotService.Encode(around, ScreenshotFormatEnum.PNG, 100),
            };

            return new Candidate(element, element.Text.ToUpperInvariant(), picture);
        }

        private static bool Moved(List<ClickedElementDto> group)
        {
            ClickedElementDto first = group[0];

            return group.Any(x =>
                Math.Abs(x.X - first.X) > MOVED_PIXELS ||
                Math.Abs(x.Y - first.Y) > MOVED_PIXELS);
        }


        // ================================================================
        // Private types
        // ================================================================

        private sealed record Candidate(ClickedElementDto Element, string Text, ElementPicture Picture);
    }
}
