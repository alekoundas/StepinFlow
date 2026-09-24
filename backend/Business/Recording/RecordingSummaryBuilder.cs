using Core.Enums;
using Core.Models.Dtos;
using Core.Ports;

namespace Business.Recording
{
    /// <summary>
    /// Turns a finished recording into something a model can read.
    ///
    /// The only thing measured here is which clicks hit the same thing. Everything else a model can
    /// work out from the words - what order things happened in, how long the gaps were - but "these
    /// twenty eight clicks were all the same icon in different places" cannot be read off a list of
    /// coordinates, and asking a model to compare twenty eight pictures is both unreliable and more
    /// tokens than it can hold.
    ///
    /// It is deliberately shy. Two clicks that happen to match are a coincidence, and a target
    /// invented here becomes a loop that does the wrong thing quietly - where a target missed only
    /// means the draft has more steps in it than it needed. So the thresholds are set to miss
    /// rather than to invent.
    ///
    /// Whether two pictures show the same thing is asked of <see cref="IOpenCvService"/>. What to
    /// do about the answer is the part that belongs here.
    /// </summary>
    public static class RecordingSummaryBuilder
    {
        // Below this a target is a coincidence. Two is never enough to call a pattern.
        private const int _minimumClicks = 2;

        // Far enough apart that it is a different place rather than an imprecise hand.
        private const int _movedPixels = 12;

        public static RecordingSummaryDto Build(IReadOnlyList<RecordedActionDto> actions, Func<int, byte[]?> screenshotOf, IOpenCvService openCvService)
        {
            RecordingSummaryDto summary = new RecordingSummaryDto
            {
                WindowTitle = SharedWindowTitle(actions),
            };

            foreach (RecordedActionDto action in actions)
            {
                summary.Actions.Add(new RecordedActionSummaryDto
                {
                    Index = action.Index,
                    Kind = action.Kind.ToString(),
                    Summary = action.Summary,
                    PauseMilliseconds = action.PauseMilliseconds,
                });
            }

            summary.Targets.AddRange(GroupClicks(actions, screenshotOf, summary, openCvService));

            return summary;
        }


        // ================================================================
        // Private methods
        // ================================================================

        // The clicks that carry a picture, grouped by what they landed on.
        private static List<ClickTargetDto> GroupClicks(IReadOnlyList<RecordedActionDto> actions, Func<int, byte[]?> screenshotOf, RecordingSummaryDto summary, IOpenCvService openCvService)
        {
            List<ClickedCrop> crops = new List<ClickedCrop>();
            List<byte[]> images = new List<byte[]>();

            foreach (RecordedActionDto action in actions)
            {
                if (action.Kind != RecordedActionKindEnum.CLICK || action.ScreenshotIndex == null)
                    continue;

                byte[]? bytes = screenshotOf(action.ScreenshotIndex.Value);
                if (bytes == null)
                    continue;

                crops.Add(new ClickedCrop(action, action.ScreenshotIndex.Value));
                images.Add(bytes);
            }

            List<ClickTargetDto> targets = new List<ClickTargetDto>();

            foreach (IReadOnlyList<int> group in openCvService.GroupSimilar(images))
            {
                if (group.Count < _minimumClicks)
                    continue;

                List<ClickedCrop> clicked = group.Select(x => crops[x]).ToList();

                ClickTargetDto target = new ClickTargetDto
                {
                    Index = targets.Count,
                    ActionIndexes = clicked.Select(x => x.Action.Index).ToList(),
                    IsMoving = Moved(clicked),
                    ScreenshotIndex = clicked[0].ScreenshotIndex,
                };

                foreach (ClickedCrop crop in clicked)
                {
                    RecordedActionSummaryDto? line = summary.Actions.FirstOrDefault(x => x.Index == crop.Action.Index);

                    if (line != null)
                        line.TargetIndex = target.Index;
                }

                targets.Add(target);
            }

            return targets;
        }

        private static bool Moved(List<ClickedCrop> group)
        {
            RecordedActionDto first = group[0].Action;

            return group.Any(x =>
                Math.Abs(x.Action.LocationX - first.LocationX) > _movedPixels ||
                Math.Abs(x.Action.LocationY - first.LocationY) > _movedPixels);
        }

        // Only worth naming when the whole recording happened in one window. Two windows and the
        // title stops being context and starts being wrong.
        private static string? SharedWindowTitle(IReadOnlyList<RecordedActionDto> actions)
        {
            List<string> titles = actions
                .Select(x => x.WindowTitle)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct()
                .ToList()!;

            return titles.Count == 1 ? titles[0] : null;
        }


        // ================================================================
        // Private types
        // ================================================================

        private sealed record ClickedCrop(RecordedActionDto Action, int ScreenshotIndex);
    }
}
