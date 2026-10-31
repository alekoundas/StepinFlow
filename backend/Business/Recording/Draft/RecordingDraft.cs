using System.Drawing;
using System.Globalization;
using Business.FlowScript.Models;
using Core.Enums;
using Core.Helpers;
using Core.Models.Business;
using Core.Models.Database;
using Core.Models.Dtos;
using Core.Ports;

namespace Business.Recording.Draft
{
    /// <summary>
    /// The draft as it is built: the flow's rows with the recording's added at a position in them, or
    /// after them. Every action's builder adds through here, so what all steps share is in one place -
    /// a name unique in the flow, the order, which branch a step goes in, the template cut for a click
    /// and the points.
    ///
    /// Everything after a search, up to the next search, goes in that search's Success branch: it
    /// only makes sense once the search found what it waited for. Its Failure branch ends the
    /// execution, failed, with a reason, because a flow that carries on clicks blind.
    /// </summary>
    public sealed class RecordingDraft
    {
        // Three times what the tester waited, never under 10 s - the machine a flow executes on is
        // often slower than the one it was recorded on - and never over 60 s.
        private const int WAIT_FACTOR = 3;
        private const int MINIMUM_TIMEOUT_MILLISECONDS = 10_000;
        private const int MAXIMUM_TIMEOUT_MILLISECONDS = 60_000;

        // Long enough for a button's label, short enough for a line of the script.
        private const int NAME_LENGTH = 40;

        private readonly RecordingResultDto _result;
        private readonly Func<int, RecordingScreenDto?> _screenAt;
        private readonly IScreenshotService _screenshotService;
        private readonly bool _includeScreenNotes;

        private readonly HashSet<string> _names;
        private readonly HashSet<string> _templateNames;
        private readonly Dictionary<FlowStep, int> _childCounts = new Dictionary<FlowStep, int>();
        private readonly Dictionary<int, FlowTemplate> _groupTemplates = new Dictionary<int, FlowTemplate>();
        private readonly List<string> _stages = new List<string>();

        // Where the recording goes: under this step, or at the top of the flow when null. The steps
        // already there after the position move down one for each step added beside them.
        private readonly FlowStep? _start;
        private readonly List<FlowStep> _after;
        private int _startCount;

        public RecordingDraft(FlowScriptSchema schema, RecordingResultDto result, Func<int, RecordingScreenDto?> screenAt, IScreenshotService screenshotService, FlowStep? under, int? index, bool includeScreenNotes)
        {
            Schema = schema;
            _result = result;
            _screenAt = screenAt;
            _screenshotService = screenshotService;
            _includeScreenNotes = includeScreenNotes;

            MainArea = schema.Areas.FirstOrDefault(x => x.IsMain);

            // Steps, areas and points share one namespace.
            IEnumerable<string> taken = schema.Steps.Select(x => x.Name).Concat(schema.Areas.Select(x => x.Name)).Concat(schema.Points.Select(x => x.Name));
            _names = new HashSet<string>(taken.Where(x => x.Length > 0), StringComparer.OrdinalIgnoreCase);
            _templateNames = schema.Steps.SelectMany(x => x.FlowStepTemplates).Select(x => x.FlowTemplate.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);

            // Numbered as the tree shows them, so the ones after the position can move down. With no
            // position, after what is already there: the setup's Launch step comes first.
            List<FlowStep> siblings = schema.Steps.Where(x => x.ParentFlowStep == under).OrderBy(x => x.OrderNumber).ToList();
            for (int i = 0; i < siblings.Count; i++)
                siblings[i].OrderNumber = i;

            _start = under;
            _startCount = Math.Clamp(index ?? siblings.Count, 0, siblings.Count);
            _after = siblings.Skip(_startCount).ToList();
            Parent = under;
        }

        public FlowScriptSchema Schema { get; }

        /// <summary>The window the flow works in. A search looks inside it, and a point is placed in it.</summary>
        public FlowArea? MainArea { get; }

        /// <summary>Where the steps added go: where the recording started, or the Success branch of the last search.</summary>
        public FlowStep? Parent { get; private set; }


        // ================================================================
        // Public methods
        // ================================================================

        public ClickedElementDto? ElementOf(RecordedActionDto action)
        {
            return _result.ClickedElements.FirstOrDefault(x => x.ActionIndex == action.Index);
        }

        /// <summary>
        /// What a press is named for: the text read on it, or when nothing was - an icon - its name
        /// as its application told Windows.
        /// </summary>
        public string TextOf(RecordedActionDto action)
        {
            string text = ElementOf(action)?.Text ?? string.Empty;
            if (text.Length > 0)
                return text;

            return _result.PressedElements.FirstOrDefault(x => x.ActionIndex == action.Index)?.Name ?? string.Empty;
        }

        /// <summary>The name, or with the first free number after it when the flow has it already.</summary>
        public string Name(string desired)
        {
            string name = FlowNameHelper.MakeUnique(desired, _names);
            _names.Add(name);

            return name;
        }

        /// <summary>A name from what was read on an element, or the fallback when nothing was.</summary>
        public static string NameFrom(string prefix, string text, string fallback)
        {
            // A name is written inside a quote.
            string shortText = ShortText(text);
            if (shortText.Length == 0)
                return fallback;

            return $"{prefix} {shortText}";
        }

        /// <summary>A stage the tester named, which begins at the next search, or at the end when none follows.</summary>
        public void BeginStage(string name)
        {
            _stages.Add(name);
        }

        /// <summary>
        /// The stages begun since the last search, as stage markers where the recording started -
        /// the top of the flow, where a marker is a stage - and steps go there from now.
        /// </summary>
        public void AddStages()
        {
            Parent = _start;

            foreach (string stage in _stages)
            {
                string oneLine = string.Join(' ', stage.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
                Add(new FlowStep { FlowStepType = FlowStepTypeEnum.STAGE_MARKER, Name = Name(oneLine) });
            }

            _stages.Clear();
        }

        /// <summary>Adds the step where steps go now, after the ones already there.</summary>
        public FlowStep Add(FlowStep step)
        {
            step.ParentFlowStep = Parent;
            step.OrderNumber = NextOrder(Parent);
            Schema.Steps.Add(step);

            return step;
        }

        /// <summary>
        /// A search that waits for the element, or for it to go, as Review said to find it: by the
        /// picture cut along its rectangle, or by the words read inside it, an area of its own in
        /// the main area. Named from what it looks for, the suffix after. Null when there is nothing
        /// to cut the picture from or to place the area by, and the caller does without.
        /// </summary>
        public FlowStep? AddSearchFor(RecordedActionDto action, ReviewedActionDto review, string prefix, string fallback, string suffix, SearchModeEnum mode)
        {
            // Words left empty are no words, so it goes by the picture.
            string words = Quotable(review.SearchText ?? string.Empty);
            if (review.SearchBy == SearchByEnum.TEXT && words.Length > 0)
            {
                string name = Name(NameFrom(prefix, words, fallback) + suffix);
                FlowArea? area = TextArea(action, review, $"{name} area");
                if (area == null)
                    return null;

                return AddWaiting(action, new FlowStep
                {
                    FlowStepType = FlowStepTypeEnum.SEARCH_TEXT,
                    Name = name,
                    SearchMode = mode,
                    FlowArea = area,
                    ConditionType = ConditionTypeEnum.CONTAINS,
                    ConditionText = words,
                });
            }

            FlowStepTemplate? template = Template(action, review);
            if (template == null)
                return null;

            string text = TextOf(action);
            return AddWaiting(action, new FlowStep
            {
                FlowStepType = FlowStepTypeEnum.SEARCH_IMAGE,
                Name = Name(NameFrom(prefix, text, fallback) + suffix),
                SearchMode = mode,
                FlowArea = MainArea,
                FlowStepTemplates = new List<FlowStepTemplate> { template },
            });
        }

        // Where the recording started, with its branches. What comes after it goes in its Success
        // branch, until the next search.
        private FlowStep AddWaiting(RecordedActionDto action, FlowStep search)
        {
            int timeout = TimeoutFor(action.WaitedMilliseconds);
            search.TimeoutMilliseconds = timeout;
            search.CodeComment = $"recorded after a {Seconds(action.WaitedMilliseconds)} wait";

            if (_includeScreenNotes)
            {
                foreach (string note in ScreenNoteWriter.NotesFor(action.Index, _result))
                    search.CodeComment += $"\n{note}";
            }

            string failed = $"{search.Name} found nothing within {Seconds(timeout)}";
            if (search.SearchMode == SearchModeEnum.WAIT_UNTIL_NOT_FOUND)
                failed = $"{search.Name}: still there after {Seconds(timeout)}";

            AddStages();
            Add(search);

            FlowStep success = Branch(search, FlowStepTypeEnum.SUCCESS, 0);
            FlowStep failure = Branch(search, FlowStepTypeEnum.FAILURE, 1);

            Parent = failure;
            Add(new FlowStep
            {
                FlowStepType = FlowStepTypeEnum.END_EXECUTION,
                Message = failed,
            });

            Parent = success;
            return search;
        }

        /// <summary>
        /// The template for a click, cut from the main area at its press along the rectangle Review
        /// settled on. Clicks that hit the same picture link to one. Null when there is nothing to cut
        /// it from, and the click goes to its spot instead.
        /// </summary>
        public FlowStepTemplate? Template(RecordedActionDto action, ReviewedActionDto review)
        {
            int? group = GroupOf(action);
            if (group != null && _groupTemplates.TryGetValue(group.Value, out FlowTemplate? shared))
                return new FlowStepTemplate { FlowTemplate = shared };

            RecordingScreenDto? screen = _screenAt(action.Index);
            if (screen == null || screen.ActionIndex != action.Index)
                return null;

            ClickedElementDto? element = ElementOf(action);
            Rectangle? reviewed = RectangleOf(review, element);
            if (reviewed == null)
                return null;

            Rectangle rectangle = reviewed.Value;
            RawImage image = _screenshotService.Decode(screen.Image);
            Rectangle whole = new Rectangle(0, 0, image.Width, image.Height);
            RawImage cut = RawImageHelper.Crop(image, whole, rectangle);
            if (cut.IsEmpty)
                return null;

            // Where in the template the click landed, so the move goes to that spot of the element.
            int clickX = Math.Clamp(action.LocationX - screen.X - rectangle.X, 0, cut.Width - 1);
            int clickY = Math.Clamp(action.LocationY - screen.Y - rectangle.Y, 0, cut.Height - 1);

            FlowTemplate picture = new FlowTemplate
            {
                Name = TemplateName(),
                TemplateImage = _screenshotService.Encode(cut, ScreenshotFormatEnum.PNG, 100),
                ClickOffsetX = clickX,
                ClickOffsetY = clickY,
                AuthoredFlowAreaWidth = screen.Width,
                AuthoredFlowAreaHeight = screen.Height,
                AuthoredDpi = screen.Dpi,
            };

            if (group != null)
                _groupTemplates[group.Value] = picture;

            return new FlowStepTemplate { FlowTemplate = picture };
        }

        /// <summary>
        /// A point where the tester's cursor was: inside the main area at the screen of that action,
        /// or on the screen when there is none or it fell outside it - which is not portable, and the
        /// script says so by writing "on screen".
        /// </summary>
        public FlowPoint Point(string desired, RecordedActionDto action, int screenX, int screenY)
        {
            RecordingScreenDto? screen = _screenAt(action.Index);
            Rectangle bounds = Rectangle.Empty;
            if (screen != null)
                bounds = new Rectangle(screen.X, screen.Y, screen.Width, screen.Height);

            FlowPoint point = new FlowPoint
            {
                Name = Name(desired),
                OffsetMode = AreaSizingModeEnum.ABSOLUTE_PX,
                LocationX = screenX,
                LocationY = screenY,
            };

            if (screen != null && MainArea != null && bounds.Contains(screenX, screenY))
            {
                point.FlowArea = MainArea;
                point.LocationX = screenX - screen.X;
                point.LocationY = screenY - screen.Y;
                point.AuthoredDpi = screen.Dpi;
            }

            Schema.Points.Add(point);
            return point;
        }

        /// <summary>An input with what was typed as its default. A secret keeps no value at all.</summary>
        public FlowCsvColumn Input(string desired, string value, bool isSecret)
        {
            HashSet<string> taken = Schema.Inputs.Select(x => x.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);

            string name = desired;
            for (int suffix = 2; taken.Contains(name); suffix++)
                name = $"{desired}_{suffix}";

            FlowCsvColumn input = new FlowCsvColumn
            {
                Name = name,
                DefaultValue = isSecret ? string.Empty : value,
                IsSecret = isSecret,
                OrderNumber = Schema.Inputs.Count,
            };

            Schema.Inputs.Add(input);
            return input;
        }

        /// <summary>The input by that name, the flow's or one this recording added, or a new one with no value yet.</summary>
        public FlowCsvColumn InputNamed(string name)
        {
            FlowCsvColumn? input = Schema.Inputs.FirstOrDefault(x => string.Equals(x.Name, name, StringComparison.OrdinalIgnoreCase));
            if (input != null)
                return input;

            return Input(name, string.Empty, false);
        }

        public static int TimeoutFor(int waitedMilliseconds)
        {
            int scaled = waitedMilliseconds * WAIT_FACTOR;
            return Math.Clamp(scaled, MINIMUM_TIMEOUT_MILLISECONDS, MAXIMUM_TIMEOUT_MILLISECONDS);
        }


        // ================================================================
        // Private methods
        // ================================================================

        private FlowStep Branch(FlowStep search, FlowStepTypeEnum type, int order)
        {
            FlowStep branch = new FlowStep { FlowStepType = type, ParentFlowStep = search, OrderNumber = order };
            _childCounts[search] = order + 1;
            Schema.Steps.Add(branch);

            return branch;
        }

        private int NextOrder(FlowStep? parent)
        {
            if (parent == null || parent == _start)
            {
                foreach (FlowStep later in _after)
                    later.OrderNumber++;

                return _startCount++;
            }

            _childCounts.TryGetValue(parent, out int count);
            _childCounts[parent] = count + 1;

            return count;
        }

        private int? GroupOf(RecordedActionDto action)
        {
            ClickGroupDto? group = _result.ClickGroups.FirstOrDefault(x => x.ActionIndexes.Contains(action.Index));
            if (group == null)
                return null;

            return group.ActionIndexes[0];
        }

        // The rectangle Review left, as an area inside the main area at the DPI of that press, placed
        // as a point is. A text search succeeds with its area's centre, so a click lands there.
        private FlowArea? TextArea(RecordedActionDto action, ReviewedActionDto review, string desired)
        {
            RecordingScreenDto? screen = _screenAt(action.Index);
            if (MainArea == null || screen == null || screen.ActionIndex != action.Index)
                return null;

            Rectangle? rectangle = RectangleOf(review, ElementOf(action));
            if (rectangle == null)
                return null;

            FlowArea area = new FlowArea
            {
                Name = Name(desired),
                Type = FlowAreaTypeEnum.CUSTOM,
                ParentFlowArea = MainArea,
                SizingMode = AreaSizingModeEnum.ABSOLUTE_PX,
                LocationX = rectangle.Value.X,
                LocationY = rectangle.Value.Y,
                Width = rectangle.Value.Width,
                Height = rectangle.Value.Height,
                AuthoredDpi = screen.Dpi,
            };

            Schema.Areas.Add(area);
            return area;
        }

        /// <summary>Text written inside a quote, on one line and without the quote's own brackets.</summary>
        public static string Quotable(string text)
        {
            string unquoted = text.Replace("<[", " ").Replace("]>", " ");
            return string.Join(' ', unquoted.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
        }

        /// <summary>Text read on an element as a name holds it: quotable, and cut to a name's length.</summary>
        public static string ShortText(string text)
        {
            string quotable = Quotable(text);
            if (quotable.Length > NAME_LENGTH)
                quotable = quotable[..NAME_LENGTH].TrimEnd();

            return quotable;
        }

        // As Review left it, or as the model found it when Review said nothing.
        private static Rectangle? RectangleOf(ReviewedActionDto review, ClickedElementDto? element)
        {
            if (review.ElementX != null && review.ElementY != null && review.ElementWidth > 0 && review.ElementHeight > 0)
                return new Rectangle(review.ElementX.Value, review.ElementY.Value, review.ElementWidth.Value, review.ElementHeight.Value);

            if (element == null)
                return null;

            return new Rectangle(element.X, element.Y, element.Width, element.Height);
        }

        private string TemplateName()
        {
            string name = FileNameHelper.GenerateTemplateFileName();
            while (_templateNames.Contains(name))
                name = FileNameHelper.GenerateTemplateFileName();

            _templateNames.Add(name);
            return name;
        }

        private static string Seconds(int milliseconds)
        {
            double seconds = milliseconds / 1000d;
            return $"{seconds.ToString("0.0", CultureInfo.InvariantCulture)}s";
        }
    }
}
