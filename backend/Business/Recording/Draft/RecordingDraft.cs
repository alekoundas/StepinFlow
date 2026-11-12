using System.Drawing;
using System.Globalization;
using Business.FlowScript.Models;
using Business.Recording.Draft.Models;
using Core.Enums;
using Core.Helpers;
using Core.Models.Business;
using Core.Models.Database;
using Core.Models.Dtos;
using Core.Ports;

namespace Business.Recording.Draft
{
    /// <summary>
    /// The draft as it is built: what the recording became, entry by entry in the order it was
    /// done, beside the flow it goes into. Every action's builder adds through here, so what all
    /// steps share is in one place - a name unique in the flow, the template cut for a click, the
    /// points and the inputs.
    ///
    /// It holds what the steps are and never where they sit: DraftLayout gives each entry its
    /// parent and its order, and is asked again whenever an entry was added or renamed - a stage
    /// or a check a model proposed, a name it wrote. So nothing a model says can move, drop or
    /// reorder what was recorded: it only ever changes words on entries that are already here, or
    /// adds one between two of them.
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

        // What a press sits in rather than what was pressed: its name says where, not what.
        private static readonly HashSet<string> SURROUNDING_ROLES = ["", "dialog", "window", "document", "group", "list", "tool bar", "data grid", "table", "tree", "pane", "menu bar", "status bar", "title bar", "custom"];

        private readonly RecordingResultDto _result;
        private readonly Func<int, RecordingScreenDto?> _screenAt;
        private readonly IScreenshotService _screenshotService;
        private readonly DraftLayout _layout;

        private readonly HashSet<string> _names;
        private readonly HashSet<string> _templateNames;
        private readonly Dictionary<int, FlowTemplate> _groupTemplates = new Dictionary<int, FlowTemplate>();
        private readonly List<string> _stages = new List<string>();
        private readonly List<FlowCsvColumn> _addedInputs = new List<FlowCsvColumn>();

        private int _actionIndex = -1;

        public RecordingDraft(FlowScriptSchema schema, RecordingResultDto result, Func<int, RecordingScreenDto?> screenAt, IScreenshotService screenshotService, FlowStep? under, int? index)
        {
            Schema = schema;
            _result = result;
            _screenAt = screenAt;
            _screenshotService = screenshotService;
            _layout = new DraftLayout(schema, under, index);

            MainArea = schema.Areas.FirstOrDefault(x => x.IsMain);

            // Steps, areas, points and inputs share one namespace.
            IEnumerable<string> taken = schema.Steps.Select(x => x.Name)
                .Concat(schema.Areas.Select(x => x.Name))
                .Concat(schema.Points.Select(x => x.Name))
                .Concat(schema.Inputs.Select(x => x.Name));
            _names = new HashSet<string>(taken.Where(x => x.Length > 0), StringComparer.OrdinalIgnoreCase);
            _templateNames = schema.Steps.SelectMany(x => x.FlowStepTemplates).Select(x => x.FlowTemplate.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
        }

        public FlowScriptSchema Schema { get; }

        /// <summary>The window the flow works in. A search looks inside it, and a point is placed in it.</summary>
        public FlowArea? MainArea { get; }

        /// <summary>What the recording became, in the order it was done.</summary>
        public List<DraftItem> Items { get; } = [];

        /// <summary>The inputs this recording added, which the flow did not have.</summary>
        public IReadOnlyList<FlowCsvColumn> AddedInputs { get { return _addedInputs; } }


        // ================================================================
        // Public methods - what every builder adds through
        // ================================================================

        /// <summary>The action the entries added from now came from.</summary>
        public void StartAction(RecordedActionDto action)
        {
            _actionIndex = action.Index;
        }

        public ClickedElementDto? ElementOf(RecordedActionDto action)
        {
            return _result.ClickedElements.FirstOrDefault(x => x.ActionIndex == action.Index);
        }

        /// <summary>
        /// What a press is named for, the first of these there is: its name as its application told
        /// Windows, when that names the thing itself rather than what it sits in; the words read on
        /// it - the whole line under the click first, where its rectangle is only a box around the
        /// click and cuts the words; what the local model called it; and last what it sits in.
        /// </summary>
        public string TextOf(RecordedActionDto action)
        {
            PressedElementDto? pressed = _result.PressedElements.FirstOrDefault(x => x.ActionIndex == action.Index);
            if (pressed != null && pressed.Name.Length > 0 && !SURROUNDING_ROLES.Contains(pressed.Role))
                return pressed.Name;

            ClickedElementDto? element = ElementOf(action);
            string read = element?.Text ?? string.Empty;
            string line = element?.LineText ?? string.Empty;

            if (element?.FoundBy == ElementFoundByEnum.FALLBACK && line.Length > 0)
                return line;

            if (read.Length > 0)
                return read;

            if (line.Length > 0)
                return line;

            string seen = _result.ScreenDescriptions.FirstOrDefault(x => x.ActionIndex == action.Index)?.Element ?? string.Empty;
            if (seen.Length > 0)
                return seen;

            return pressed?.Name ?? string.Empty;
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

        /// <summary>The stages begun since the last search, as stage markers before whatever is added next.</summary>
        public void AddStages()
        {
            foreach (string stage in _stages)
            {
                string oneLine = string.Join(' ', stage.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
                FlowStep marker = new FlowStep { FlowStepType = FlowStepTypeEnum.STAGE_MARKER, Name = Name(oneLine) };

                Items.Add(new DraftItem { Kind = DraftItemKindEnum.STAGE, Step = marker, ActionIndex = _actionIndex, IsTesters = true });
            }

            _stages.Clear();
        }

        /// <summary>Adds the step after everything added so far.</summary>
        public FlowStep Add(FlowStep step)
        {
            Items.Add(new DraftItem { Kind = DraftItemKindEnum.STEP, Step = step, ActionIndex = _actionIndex });
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
            // Words left empty are no words, so it goes by the picture, unless its rule needs none.
            string words = Quotable(review.SearchText ?? string.Empty);
            ConditionTypeEnum condition = review.ConditionType ?? ConditionTypeEnum.CONTAINS;
            bool needsWords = condition != ConditionTypeEnum.IS_EMPTY && condition != ConditionTypeEnum.IS_NOT_EMPTY;

            if (review.SearchBy == SearchByEnum.TEXT && (words.Length > 0 || !needsWords))
            {
                // A number, a pattern or no words at all names nothing, so what was read on it does.
                string named = words;
                if (condition != ConditionTypeEnum.CONTAINS && condition != ConditionTypeEnum.EQUALS)
                    named = TextOf(action);

                string name = Name(NameFrom(prefix, named, fallback) + suffix);
                FlowArea? area = TextArea(action, review, AreaNameOf(name));
                if (area == null)
                    return null;

                string end = string.Empty;
                if (condition == ConditionTypeEnum.BETWEEN)
                    end = Quotable(review.ConditionTextEnd ?? string.Empty);

                return AddWaiting(action, new FlowStep
                {
                    FlowStepType = FlowStepTypeEnum.SEARCH_TEXT,
                    Name = name,
                    SearchMode = mode,
                    FlowArea = area,
                    ConditionType = condition,
                    ConditionText = needsWords ? words : string.Empty,
                    ConditionTextEnd = end,
                });
            }

            FlowStepTemplate? template = Template(action, review);
            if (template == null)
                return null;

            // A picture is waited for until it stands still, so a click below lands where it came to rest.
            SearchModeEnum imageMode = mode;
            if (mode == SearchModeEnum.WAIT_UNTIL_FOUND)
                imageMode = SearchModeEnum.WAIT_UNTIL_STILL;

            string text = TextOf(action);
            return AddWaiting(action, new FlowStep
            {
                FlowStepType = FlowStepTypeEnum.SEARCH_IMAGE,
                Name = Name(NameFrom(prefix, text, fallback) + suffix),
                SearchMode = imageMode,
                FlowArea = MainArea,
                FlowStepTemplates = new List<FlowStepTemplate> { template },
            });
        }

        /// <summary>
        /// A step in a search's Failure branch, ahead of the End Execution the branch ends with, so
        /// it is done before the execution stops.
        /// </summary>
        public FlowStep AddToFailure(FlowStep search, FlowStep step)
        {
            DraftItem item = Items.First(x => x.Kind == DraftItemKindEnum.SEARCH && x.Step == search);
            item.BeforeEnd.Add(step);

            return step;
        }

        /// <summary>A Find All Images, with what is done at each hit: clicks on one picture in several places, as one step.</summary>
        public FlowStep AddEvery(FlowStep findAll, IReadOnlyList<FlowStep> eachHit)
        {
            DraftItem item = new DraftItem { Kind = DraftItemKindEnum.EVERY, Step = findAll, ActionIndex = _actionIndex };
            item.EachHit.AddRange(eachHit);
            Items.Add(item);

            return findAll;
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
            FlowCsvColumn input = new FlowCsvColumn
            {
                Name = FreeInputName(desired),
                DefaultValue = isSecret ? string.Empty : value,
                IsSecret = isSecret,
                OrderNumber = Schema.Inputs.Count,
            };

            Schema.Inputs.Add(input);
            _addedInputs.Add(input);

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

        /// <summary>Gives every entry its place in the flow, and returns the flow.</summary>
        public FlowScriptSchema Lay()
        {
            _layout.Lay(Items);
            return Schema;
        }


        // ================================================================
        // Public methods - changed once it is built
        // ================================================================

        /// <summary>
        /// Another name for a step of the draft, numbered when the flow has it already. An area made
        /// for the step's words, and a failure message that only named the step, follow it.
        /// </summary>
        public void Rename(FlowStep step, string desired)
        {
            DraftItem? search = Items.FirstOrDefault(x => x.Kind == DraftItemKindEnum.SEARCH && x.Step == step);
            bool isNamedInFailure = search?.End != null && search.End.Message == FailureOf(step);
            FlowArea? ownArea = OwnAreaOf(step);

            _names.Remove(step.Name);
            step.Name = Name(desired);

            if (ownArea != null)
            {
                _names.Remove(ownArea.Name);
                ownArea.Name = Name(AreaNameOf(step.Name));
            }

            if (isNamedInFailure)
                search!.End!.Message = FailureOf(step);
        }

        /// <summary>
        /// Another name for an input this recording added, with every step that types it. False for
        /// one the flow already had, or a name already taken.
        /// </summary>
        public bool RenameInput(FlowCsvColumn input, string desired)
        {
            bool isTaken = Schema.Inputs.Any(x => x != input && string.Equals(x.Name, desired, StringComparison.OrdinalIgnoreCase));
            if (!_addedInputs.Contains(input) || isTaken || desired.Length == 0)
                return false;

            string typed = $"{{{{{input.Name}}}}}";
            string renamed = $"{{{{{desired}}}}}";

            foreach (FlowStep step in Items.Select(x => x.Step).Where(x => x.FlowStepType == FlowStepTypeEnum.KEYBOARD_INPUT && x.KeyboardInputText == typed))
                step.KeyboardInputText = renamed;

            input.Name = desired;
            return true;
        }

        /// <summary>
        /// A stage marker before a search, which a model proposed. Null when one already begins
        /// there: the tester's stands, and two stages cannot begin at one step.
        /// </summary>
        public DraftItem? AddStageBefore(DraftItem search, string name, string comment)
        {
            int at = Items.IndexOf(search);
            if (at < 0 || (at > 0 && Items[at - 1].Kind == DraftItemKindEnum.STAGE))
                return null;

            FlowStep marker = new FlowStep { FlowStepType = FlowStepTypeEnum.STAGE_MARKER, Name = Name(name), CodeComment = comment };
            DraftItem stage = new DraftItem { Kind = DraftItemKindEnum.STAGE, Step = marker };
            Items.Insert(at, stage);

            return stage;
        }

        /// <summary>
        /// A check for words in the main area, which a model proposed, after everything the search
        /// before it does: it waits for them, and ends the execution when they never show. Null with
        /// no main area to read them in.
        /// </summary>
        public DraftItem? AddCheckAfter(DraftItem search, string words, string name, string comment, string notFound)
        {
            int at = Items.IndexOf(search);
            if (at < 0 || MainArea == null)
                return null;

            // After what the search does, up to the next search, stage or repeat.
            int end = at + 1;
            while (end < Items.Count && Items[end].Kind == DraftItemKindEnum.STEP)
                end++;

            int timeout = TimeoutFor(0);
            FlowStep check = new FlowStep
            {
                FlowStepType = FlowStepTypeEnum.SEARCH_TEXT,
                Name = Name(name),
                SearchMode = SearchModeEnum.WAIT_UNTIL_FOUND,
                FlowArea = MainArea,
                ConditionType = ConditionTypeEnum.CONTAINS,
                ConditionText = words,
                TimeoutMilliseconds = timeout,
                CodeComment = comment,
            };

            string failed = notFound;
            if (failed.Length == 0)
                failed = FailureOf(check);

            DraftItem item = new DraftItem
            {
                Kind = DraftItemKindEnum.SEARCH,
                Step = check,
                End = new FlowStep { FlowStepType = FlowStepTypeEnum.END_EXECUTION, Message = failed },
                IsCheck = true,
                IsProposed = true,
            };

            Items.Insert(end, item);
            return item;
        }

        /// <summary>A QA test says when it has passed: an End Execution passed, after everything recorded.</summary>
        public FlowStep AddPassed(string message)
        {
            FlowStep passed = new FlowStep { FlowStepType = FlowStepTypeEnum.END_EXECUTION, EndExecutionAsSuccess = true, Message = message };
            Items.Add(new DraftItem { Kind = DraftItemKindEnum.STEP, Step = passed });

            return passed;
        }

        /// <summary>What a search's Failure branch says when it only names the search and how long it waited.</summary>
        public static string FailureOf(FlowStep search)
        {
            if (search.SearchMode == SearchModeEnum.WAIT_UNTIL_NOT_FOUND)
                return $"{search.Name}: still there after {Seconds(search.TimeoutMilliseconds)}";

            return $"{search.Name} found nothing within {Seconds(search.TimeoutMilliseconds)}";
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


        // ================================================================
        // Private methods
        // ================================================================

        // What comes after it goes in its Success branch, until the next search, and its Failure
        // branch ends the execution, failed, with a reason: a flow that carries on clicks blind.
        private FlowStep AddWaiting(RecordedActionDto action, FlowStep search)
        {
            search.TimeoutMilliseconds = TimeoutFor(action.WaitedMilliseconds);
            search.CodeComment = $"recorded after a {Seconds(action.WaitedMilliseconds)} wait";

            AddStages();
            Items.Add(new DraftItem
            {
                Kind = DraftItemKindEnum.SEARCH,
                Step = search,
                ActionIndex = action.Index,
                End = new FlowStep { FlowStepType = FlowStepTypeEnum.END_EXECUTION, Message = FailureOf(search) },
                Page = _result.ScreenPages.FirstOrDefault(x => x.ActionIndex == action.Index)?.Page,
                IsCheck = action.Kind == RecordedActionKindEnum.CHECK,
            });

            return search;
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

        private static string AreaNameOf(string searchName)
        {
            return $"{searchName} area";
        }

        // The area a search by its words reads them in, when it was made for that search alone.
        private FlowArea? OwnAreaOf(FlowStep step)
        {
            FlowArea? area = step.FlowArea;
            if (area == null || area == MainArea || area.Name != AreaNameOf(step.Name))
                return null;

            return area;
        }

        private string FreeInputName(string desired)
        {
            HashSet<string> taken = Schema.Inputs.Select(x => x.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);

            string name = desired;
            for (int suffix = 2; taken.Contains(name); suffix++)
                name = $"{desired}_{suffix}";

            return name;
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
