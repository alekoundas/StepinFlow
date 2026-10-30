using Business.FlowScript.Diagnostics;
using Business.FlowScript.Models;
using Core.Enums;
using Core.Models.Database;

namespace Business.Recording.Generation
{
    /// <summary>
    /// What a model may not do to a recording: add, drop or reorder a click, a drag, a scroll, typed
    /// text or a key. The draft and the answer are both read in the order an execution that finds
    /// everything goes - down each search's Success branch, never its Failure - and compared one by
    /// one. Only the prompt asked for it before, and restructuring is where a model slips.
    ///
    /// A Find All Images' Success branch, or a loop's, is done once per hit or pass, so what it holds
    /// stands for one or more of it in a row in the draft: four clicks on one picture folded into a
    /// Find All Images are still the four. Typed text is compared as typed text, whatever it types,
    /// since an input may be renamed. What each click lands on is the searches' business, and an
    /// answer can name only the templates the draft cut.
    /// </summary>
    public static class RecordedActionsCheck
    {
        private const string KEEP = "Keep every click, drag, scroll, typed text and key the draft has, in its order: never add one, drop one or move one.";

        public static List<Diagnostic> Check(FlowScriptSchema draft, FlowScriptSchema answer)
        {
            List<Act> drafted = Flatten(ItemsUnder(draft, null, string.Empty));
            List<Item> answered = ItemsUnder(answer, null, string.Empty);

            string? differs = Match(answered, 0, drafted, 0, out int end);
            if (differs == null && end < drafted.Count)
                differs = $"The answer leaves out the draft's action {end + 1}, {drafted[end].What}{Under(drafted[end])}, and every one after it.";

            if (differs == null)
                return [];

            return [Diagnostic.Error(DiagnosticCodeEnum.ACTIONS_CHANGED, 1, 1, $"{differs} {KEEP}")];
        }


        // ================================================================
        // Private methods
        // ================================================================

        // The answer's items from first on against the draft's actions from next on: null when they
        // match, with end past the last action matched, else what differs first.
        private static string? Match(List<Item> items, int first, List<Act> drafted, int next, out int end)
        {
            end = next;
            if (first == items.Count)
                return null;

            if (items[first] is Repeat repeat)
                return MatchRepeat(repeat, items, first, drafted, next, out end);

            Act act = (Act)items[first];
            if (next >= drafted.Count)
                return $"The answer adds {act.What}{Under(act)} after the draft's last action.";

            if (act.What != drafted[next].What)
                return $"The draft's action {next + 1} is {drafted[next].What}{Under(drafted[next])}, and the answer has {act.What}{Under(act)} there.";

            return Match(items, first + 1, drafted, next + 1, out end);
        }

        // Once at least. Then as many passes as the draft goes on doing the same, fewer when what
        // comes after the repeat only matches that way - the click on Log out after three on the
        // download icon is a click as well.
        private static string? MatchRepeat(Repeat repeat, List<Item> items, int first, List<Act> drafted, int next, out int end)
        {
            end = next;

            string? differs = Match(repeat.Items, 0, drafted, next, out int passEnd);
            if (differs != null)
                return differs;

            List<int> passEnds = [passEnd];
            while (Match(repeat.Items, 0, drafted, passEnds[^1], out passEnd) == null && passEnd > passEnds[^1])
                passEnds.Add(passEnd);

            string? mostPassesDiffers = null;
            for (int passes = passEnds.Count; passes >= 1; passes--)
            {
                string? restDiffers = Match(items, first + 1, drafted, passEnds[passes - 1], out end);
                if (restDiffers == null)
                    return null;

                mostPassesDiffers ??= restDiffers;
            }

            return mostPassesDiffers;
        }

        // What an execution that finds everything does under the parent, in order.
        private static List<Item> ItemsUnder(FlowScriptSchema schema, FlowStep? parent, string under)
        {
            List<Item> items = new List<Item>();

            foreach (FlowStep step in ChildrenOf(schema, parent))
            {
                string? what = WhatIs(step);
                if (what != null)
                {
                    items.Add(new Act(what, under));
                    continue;
                }

                List<FlowStep> children = ChildrenOf(schema, step).ToList();
                bool isBranching = children.Any(x => x.FlowStepType == FlowStepTypeEnum.SUCCESS || x.FlowStepType == FlowStepTypeEnum.FAILURE);

                List<Item> inner = new List<Item>();
                if (!isBranching)
                    inner = ItemsUnder(schema, step, under);

                FlowStep? success = children.FirstOrDefault(x => x.FlowStepType == FlowStepTypeEnum.SUCCESS);
                if (isBranching && success != null)
                    inner = ItemsUnder(schema, success, step.Name);

                // A repeat that does nothing, a Find All Images that only checks, is nothing.
                bool isRepeated = step.FlowStepType == FlowStepTypeEnum.LOOP || step.SearchMode == SearchModeEnum.FIND_ALL;
                if (isRepeated && Flatten(inner).Count > 0)
                    items.Add(new Repeat(inner));
                else if (!isRepeated)
                    items.AddRange(inner);
            }

            return items;
        }

        private static IEnumerable<FlowStep> ChildrenOf(FlowScriptSchema schema, FlowStep? parent)
        {
            return schema.Steps
                .Where(x => x.ParentFlowStep == parent)
                .OrderBy(x => x.OrderNumber);
        }

        // Null for a step that does nothing to the application: a search, a move, a wait.
        private static string? WhatIs(FlowStep step)
        {
            switch (step.FlowStepType)
            {
                case FlowStepTypeEnum.CURSOR_CLICK:
                    return $"a {Words(step.CursorButtonActionType ?? CursorButtonActionTypeEnum.SINGLE_CLICK)} with the {Words(step.CursorButtonType ?? CursorButtonTypeEnum.LEFT_BUTTON)}";

                case FlowStepTypeEnum.CURSOR_DRAG:
                    return "a drag";

                case FlowStepTypeEnum.CURSOR_SCROLL:
                    return $"a scroll {Words(step.CursorScrollDirectionType ?? CursorScrollDirectionTypeEnum.DOWN)}";

                case FlowStepTypeEnum.KEYBOARD_INPUT when step.KeyboardInputType == KeyboardInputTypeEnum.TEXT:
                    return "typed text";

                case FlowStepTypeEnum.KEYBOARD_INPUT:
                    return $"the key {step.KeyboardInputText} {KeyAction(step.KeyboardKeyActionType ?? KeyboardKeyActionTypeEnum.PRESS)}";

                default:
                    return null;
            }
        }

        private static string KeyAction(KeyboardKeyActionTypeEnum action)
        {
            switch (action)
            {
                case KeyboardKeyActionTypeEnum.HOLD:
                    return "held down";

                case KeyboardKeyActionTypeEnum.RELEASE:
                    return "let go";

                default:
                    return "pressed";
            }
        }

        private static string Words<TEnum>(TEnum value) where TEnum : struct, Enum
        {
            return value.ToString().Replace('_', ' ').ToLowerInvariant();
        }

        private static string Under(Act act)
        {
            if (act.Under.Length == 0)
                return string.Empty;

            return $" under <[ {act.Under} ]>";
        }

        // A repeat counted once, as the draft has none of its own.
        private static List<Act> Flatten(List<Item> items)
        {
            List<Act> acts = new List<Act>();

            foreach (Item item in items)
            {
                if (item is Act act)
                    acts.Add(act);
                else
                    acts.AddRange(Flatten(((Repeat)item).Items));
            }

            return acts;
        }


        // ================================================================
        // Private types
        // ================================================================

        private abstract record Item;
        private sealed record Act(string What, string Under) : Item;
        private sealed record Repeat(List<Item> Items) : Item;
    }
}
