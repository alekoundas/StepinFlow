using System.Globalization;
using System.Text;
using Business.Recording.Draft;
using Business.Recording.Draft.Models;
using Core.Enums;
using Core.Models.Database;
using Core.Models.Dtos;

namespace Business.Recording.Generation
{
    /// <summary>
    /// A recording's draft as a model reads it: the task, then every search in the order it was
    /// recorded, each by an id of its own, with what the screen said there and what was done once
    /// it was found. Never the script: a model given only this cannot move, drop or misname a
    /// step, a point or an area, because it never writes one. It answers by the ids, and the code
    /// puts what it said on the steps.
    /// </summary>
    public sealed class RecordingOutline
    {
        // Long enough to tell one typed value from another, short enough for a line.
        private const int TYPED_LENGTH = 40;

        private RecordingOutline(string text, IReadOnlyDictionary<string, DraftItem> searches)
        {
            Text = text;
            Searches = searches;
        }

        public string Text { get; }

        /// <summary>Each search by its id: S1, S2, in the order recorded.</summary>
        public IReadOnlyDictionary<string, DraftItem> Searches { get; }


        // ================================================================
        // Public methods
        // ================================================================

        /// <summary>
        /// The outline of the draft. The recording is the one the draft was built from, so with
        /// screen content withheld its notes say only where a page began and what kind of thing was
        /// pressed.
        /// </summary>
        public static RecordingOutline Of(RecordingDraft draft, RecordingResultDto recording)
        {
            Flow flow = draft.Schema.Flow;
            StringBuilder text = new StringBuilder();

            string task = string.Join(' ', flow.Description.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
            if (task.Length == 0)
                task = "not written";

            string kind = "an automation";
            if (flow.IsQaTest)
                kind = "a QA test";

            text.AppendLine(CultureInfo.InvariantCulture, $"The task, in the tester's words: {task}");
            text.AppendLine(CultureInfo.InvariantCulture, $"The flow is called \"{flow.Name}\" and is {kind}.");
            text.AppendLine(CultureInfo.InvariantCulture, $"It works in {WindowOf(draft.MainArea)}.");
            text.AppendLine();
            text.AppendLine("The searches, in the order they were recorded:");

            Dictionary<string, DraftItem> searches = new Dictionary<string, DraftItem>(StringComparer.OrdinalIgnoreCase);
            List<DraftItem> items = draft.Items;

            for (int i = 0; i < items.Count; i++)
            {
                DraftItem item = items[i];

                if (item.Kind == DraftItemKindEnum.STAGE)
                {
                    text.AppendLine();
                    text.AppendLine(CultureInfo.InvariantCulture, $"## {item.Step.Name}");
                    continue;
                }

                if (item.Kind != DraftItemKindEnum.SEARCH)
                    continue;

                string id = $"S{searches.Count + 1}";
                searches[id] = item;

                text.AppendLine();
                text.AppendLine(CultureInfo.InvariantCulture, $"{id,-3} {item.Step.Name}{CheckOf(item)}");

                foreach (string note in ScreenNoteWriter.NotesFor(item.ActionIndex, recording).Where(x => !x.Contains("after the click", StringComparison.Ordinal)))
                    text.AppendLine(CultureInfo.InvariantCulture, $"    {note}");

                List<string> done = DoneAfter(items, i);
                if (done.Count > 0)
                    text.AppendLine(CultureInfo.InvariantCulture, $"    then: {string.Join(", ", done)}");

                foreach (string note in ScreenNoteWriter.NotesFor(item.ActionIndex, recording).Where(x => x.Contains("after the click", StringComparison.Ordinal)))
                    text.AppendLine(CultureInfo.InvariantCulture, $"    {note}");
            }

            // The same on every system, since a request's lines are counted to mark the ones read off the screen.
            return new RecordingOutline(text.ToString().ReplaceLineEndings("\n").TrimEnd(), searches);
        }


        // ================================================================
        // Private methods
        // ================================================================

        private static string WindowOf(FlowArea? mainArea)
        {
            if (mainArea == null)
                return "no window of its own";

            if (mainArea.Type == FlowAreaTypeEnum.BROWSER_TAB && mainArea.TabAddress.Length > 0)
                return $"a browser tab at {mainArea.TabAddress}";

            if (mainArea.ProcessName.Length > 0)
                return $"the window of {mainArea.ProcessName}";

            return $"the window \"{mainArea.Name}\"";
        }

        private static string CheckOf(DraftItem search)
        {
            if (!search.IsCheck)
                return string.Empty;

            if (search.Step.SearchMode == SearchModeEnum.WAIT_UNTIL_NOT_FOUND)
                return "   (a check the tester marked: waits until it has gone)";

            return "   (a check the tester marked: waits until it is there)";
        }

        // What is done once the search found what it waited for, up to the next search or stage.
        private static List<string> DoneAfter(List<DraftItem> items, int search)
        {
            List<string> done = new List<string>();

            for (int i = search + 1; i < items.Count; i++)
            {
                DraftItem item = items[i];
                if (item.Kind == DraftItemKindEnum.SEARCH || item.Kind == DraftItemKindEnum.STAGE)
                    break;

                if (item.Kind == DraftItemKindEnum.EVERY)
                {
                    done.Add("find every one like it on the screen and click each");
                    continue;
                }

                if (item.Kind == DraftItemKindEnum.READ)
                {
                    done.Add("read the text in an area");
                    foreach (string withRead in item.InSuccess.Select(WhatIs).OfType<string>())
                        done.Add($"{withRead} with it");
                    continue;
                }

                string? what = WhatIs(item.Step);
                if (what != null)
                    done.Add(what);
            }

            return done;
        }

        // In a few words. Null for a move, which only says where the next step acts.
        private static string? WhatIs(FlowStep step)
        {
            switch (step.FlowStepType)
            {
                case FlowStepTypeEnum.CURSOR_CLICK:
                    return ClickOf(step);

                case FlowStepTypeEnum.CURSOR_DRAG:
                    return "drag to a spot";

                case FlowStepTypeEnum.CURSOR_SCROLL:
                    return $"scroll {Words(step.CursorScrollDirectionType ?? CursorScrollDirectionTypeEnum.DOWN)}";

                case FlowStepTypeEnum.KEYBOARD_INPUT when step.KeyboardInputType == KeyboardInputTypeEnum.TEXT:
                    return TypedOf(step);

                case FlowStepTypeEnum.KEYBOARD_INPUT:
                    return $"{KeyAction(step.KeyboardKeyActionType ?? KeyboardKeyActionTypeEnum.PRESS)} {step.KeyboardInputText}";

                case FlowStepTypeEnum.NOTIFY:
                    return "tell Discord";

                case FlowStepTypeEnum.WAIT:
                    return $"wait {(step.WaitForMilliseconds / 1000d).ToString("0.0", CultureInfo.InvariantCulture)}s";

                case FlowStepTypeEnum.SYSTEM_COMMAND when step.ShellCommandMode == ShellCommandModeEnum.LAUNCH_APP:
                    return "open an application";

                case FlowStepTypeEnum.SYSTEM_COMMAND:
                    return "execute a command";

                default:
                    return null;
            }
        }

        private static string ClickOf(FlowStep click)
        {
            string button = string.Empty;
            if (click.CursorButtonType == CursorButtonTypeEnum.RIGHT_BUTTON)
                button = "right ";

            if (click.CursorButtonActionType == CursorButtonActionTypeEnum.DOUBLE_CLICK)
                return $"{button}double click";

            return $"{button}click";
        }

        // An input by its name, never its value. Text typed as written is in the flow anyway.
        private static string TypedOf(FlowStep step)
        {
            string text = step.KeyboardInputText;
            if (text.StartsWith("{{", StringComparison.Ordinal))
                return $"type {text}";

            if (text.Length > TYPED_LENGTH)
                text = text[..TYPED_LENGTH] + "…";

            string typed = $"type \"{text.Replace('"', '\'')}\"";
            if (step.CodeComment.Length > 0)
                typed += $" ({step.CodeComment})";

            return typed;
        }

        private static string KeyAction(KeyboardKeyActionTypeEnum action)
        {
            switch (action)
            {
                case KeyboardKeyActionTypeEnum.HOLD:
                    return "hold";

                case KeyboardKeyActionTypeEnum.RELEASE:
                    return "release";

                default:
                    return "press";
            }
        }

        private static string Words<TEnum>(TEnum value) where TEnum : struct, Enum
        {
            return value.ToString().Replace('_', ' ').ToLowerInvariant();
        }
    }
}
