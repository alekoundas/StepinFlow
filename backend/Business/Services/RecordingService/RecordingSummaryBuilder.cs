using Core.Enums;
using Core.Models.Dtos;

using OpenCvSharp;

namespace Business.Services.RecordingService
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
    /// </summary>
    public static class RecordingSummaryBuilder
    {
        // The centre of one crop, looked for anywhere in the other. Whole crops do not compare well:
        // the same icon clicked in two places carries different background with it, and the score
        // collapses on the background rather than the thing that was clicked.
        private const double _centreFraction = 0.5;

        // Clear of the noise floor rather than near it. Measured against real templates, an
        // unrelated pair scores under 0.45 and a genuine match sits well above 0.8.
        private const double _sameThing = 0.8;

        // A crop of flat colour matches every other crop of flat colour. Anything this featureless
        // is background, and background is not a thing that was clicked.
        private const double _minimumDetail = 12d;

        // Below this a target is a coincidence. Two is never enough to call a pattern.
        private const int _minimumClicks = 2;

        // Far enough apart that it is a different place rather than an imprecise hand.
        private const int _movedPixels = 12;

        public static RecordingSummaryDto Build(IReadOnlyList<RecordedActionDto> actions, Func<int, byte[]?> screenshotOf)
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

            summary.Targets.AddRange(GroupClicks(actions, screenshotOf, summary));

            return summary;
        }


        // ================================================================
        // Private methods
        // ================================================================

        // The clicks that carry a picture, grouped by what they landed on.
        private static List<ClickTargetDto> GroupClicks(IReadOnlyList<RecordedActionDto> actions, Func<int, byte[]?> screenshotOf, RecordingSummaryDto summary)
        {
            List<ClickedCrop> crops = new List<ClickedCrop>();

            foreach (RecordedActionDto action in actions)
            {
                if (action.Kind != RecordedActionKindEnum.CLICK || action.ScreenshotIndex == null)
                    continue;

                byte[]? bytes = screenshotOf(action.ScreenshotIndex.Value);
                if (bytes == null)
                    continue;

                Mat decoded = Cv2.ImDecode(bytes, ImreadModes.Color);
                if (decoded.Empty() || !HasDetail(decoded))
                {
                    decoded.Dispose();
                    continue;
                }

                crops.Add(new ClickedCrop(action, action.ScreenshotIndex.Value, decoded));
            }

            try
            {
                List<List<ClickedCrop>> groups = Cluster(crops);
                List<ClickTargetDto> targets = new List<ClickTargetDto>();

                foreach (List<ClickedCrop> group in groups)
                {
                    if (group.Count < _minimumClicks)
                        continue;

                    ClickTargetDto target = new ClickTargetDto
                    {
                        Index = targets.Count,
                        ActionIndexes = group.Select(x => x.Action.Index).ToList(),
                        IsMoving = Moved(group),
                        ScreenshotIndex = group[0].ScreenshotIndex,
                    };

                    foreach (ClickedCrop crop in group)
                    {
                        RecordedActionSummaryDto? line = summary.Actions
                            .FirstOrDefault(x => x.Index == crop.Action.Index);

                        if (line != null)
                            line.TargetIndex = target.Index;
                    }

                    targets.Add(target);
                }

                return targets;
            }
            finally
            {
                foreach (ClickedCrop crop in crops)
                    crop.Image.Dispose();
            }
        }

        // Greedy: the first crop of a group is what the rest are compared against, so a group never
        // drifts away from what started it.
        private static List<List<ClickedCrop>> Cluster(List<ClickedCrop> crops)
        {
            List<List<ClickedCrop>> groups = new List<List<ClickedCrop>>();

            foreach (ClickedCrop crop in crops)
            {
                List<ClickedCrop>? found = groups.FirstOrDefault(x => IsSameThing(x[0].Image, crop.Image));

                if (found != null)
                    found.Add(crop);
                else
                    groups.Add([crop]);
            }

            return groups;
        }

        // The middle of one, looked for anywhere in the other.
        private static bool IsSameThing(Mat first, Mat second)
        {
            if (first.Width != second.Width || first.Height != second.Height)
                return false;

            using Mat centre = new Mat(first, CentreOf(first));
            if (centre.Width < 8 || centre.Height < 8)
                return false;

            using Mat result = new Mat();
            Cv2.MatchTemplate(second, centre, result, TemplateMatchModes.CCoeffNormed);
            Cv2.PatchNaNs(result, 0d);

            result.MinMaxLoc(out double _, out double best);

            return best >= _sameThing;
        }

        private static Rect CentreOf(Mat image)
        {
            int width = (int)(image.Width * _centreFraction);
            int height = (int)(image.Height * _centreFraction);

            return new Rect((image.Width - width) / 2, (image.Height - height) / 2, width, height);
        }

        // Flat colour matches flat colour, so anything without variation is thrown away before it
        // can join a group.
        //
        // Measured on the centre rather than the whole crop, because the centre is what gets
        // compared. A click on a plain button in a busy window has a crop full of detail and a
        // middle with none, and judging the whole thing lets it through to match everything.
        private static bool HasDetail(Mat image)
        {
            using Mat centre = new Mat(image, CentreOf(image));
            using Mat grey = centre.CvtColor(ColorConversionCodes.BGR2GRAY);
            Cv2.MeanStdDev(grey, out Scalar _, out Scalar deviation);

            return deviation.Val0 >= _minimumDetail;
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

        private sealed record ClickedCrop(RecordedActionDto Action, int ScreenshotIndex, Mat Image);
    }
}
