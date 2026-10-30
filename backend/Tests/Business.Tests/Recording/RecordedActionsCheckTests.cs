using Business.FlowScript.Diagnostics;
using Business.FlowScript.Models;
using Business.Recording.Generation;
using Core.Enums;
using Core.Models.Database;

namespace Business.Tests.Recording
{
    /// <summary>
    /// A model's answer keeps every recorded click, drag, scroll, typed text and key, in order,
    /// however it names, nests and folds them.
    /// </summary>
    public sealed class RecordedActionsCheckTests
    {
        private static FlowStep Add(FlowScriptSchema schema, FlowStep? parent, FlowStep step)
        {
            step.ParentFlowStep = parent;
            step.OrderNumber = schema.Steps.Count(x => x.ParentFlowStep == parent);
            schema.Steps.Add(step);

            return step;
        }

        // A search with both branches, a failed one ending the execution. Gives its Success branch.
        private static FlowStep Search(FlowScriptSchema schema, FlowStep? parent, string name, SearchModeEnum mode = SearchModeEnum.WAIT_UNTIL_FOUND)
        {
            FlowStep search = Add(schema, parent, new FlowStep { FlowStepType = FlowStepTypeEnum.SEARCH_IMAGE, Name = name, SearchMode = mode });
            FlowStep success = Add(schema, search, new FlowStep { FlowStepType = FlowStepTypeEnum.SUCCESS });
            FlowStep failure = Add(schema, search, new FlowStep { FlowStepType = FlowStepTypeEnum.FAILURE });
            Add(schema, failure, new FlowStep { FlowStepType = FlowStepTypeEnum.END_EXECUTION });

            return success;
        }

        private static void Click(FlowScriptSchema schema, FlowStep? parent)
        {
            Add(schema, parent, new FlowStep { FlowStepType = FlowStepTypeEnum.CURSOR_RELOCATE });
            Add(schema, parent, new FlowStep { FlowStepType = FlowStepTypeEnum.CURSOR_CLICK, CursorButtonType = CursorButtonTypeEnum.LEFT_BUTTON, CursorButtonActionType = CursorButtonActionTypeEnum.SINGLE_CLICK });
        }

        private static void Type(FlowScriptSchema schema, FlowStep? parent)
        {
            Add(schema, parent, new FlowStep { FlowStepType = FlowStepTypeEnum.KEYBOARD_INPUT, KeyboardInputType = KeyboardInputTypeEnum.TEXT, KeyboardInputText = "{{email}}" });
        }

        private static void Press(FlowScriptSchema schema, FlowStep? parent, string key)
        {
            Add(schema, parent, new FlowStep { FlowStepType = FlowStepTypeEnum.KEYBOARD_INPUT, KeyboardInputType = KeyboardInputTypeEnum.COMBINATION, KeyboardInputText = key, KeyboardKeyActionType = KeyboardKeyActionTypeEnum.PRESS });
        }

        // As the plain draft has it: every search at the top, what followed it in its Success branch.
        private static FlowScriptSchema Draft()
        {
            FlowScriptSchema draft = new FlowScriptSchema();
            Click(draft, Search(draft, null, "Find Login"));

            FlowStep email = Search(draft, null, "Find Email");
            Click(draft, email);
            Type(draft, email);

            FlowStep submit = Search(draft, null, "Find Submit");
            Click(draft, submit);
            Press(draft, submit, "Enter");

            return draft;
        }

        private static string Refusal(FlowScriptSchema answer)
        {
            Diagnostic refused = RecordedActionsCheck.Check(Draft(), answer).ShouldHaveSingleItem();
            refused.Code.ShouldBe(DiagnosticCodeEnum.ACTIONS_CHANGED);

            return refused.Message;
        }

        [Fact]
        public void Renamed_and_nested_by_page_with_every_action_in_order_passes()
        {
            FlowScriptSchema answer = new FlowScriptSchema();
            FlowStep login = Search(answer, null, "Find the login button");
            Click(answer, login);

            FlowStep email = Search(answer, login, "Find the email field");
            Click(answer, email);
            Type(answer, email);

            FlowStep submit = Search(answer, email, "Find Submit");
            Click(answer, submit);
            Press(answer, submit, "Enter");

            RecordedActionsCheck.Check(Draft(), answer).ShouldBeEmpty();
        }

        [Fact]
        public void An_action_left_out_is_refused_where_the_draft_had_it()
        {
            FlowScriptSchema answer = new FlowScriptSchema();
            Click(answer, Search(answer, null, "Find Login"));
            Click(answer, Search(answer, null, "Find Email"));

            FlowStep submit = Search(answer, null, "Find Submit");
            Click(answer, submit);
            Press(answer, submit, "Enter");

            Refusal(answer).ShouldStartWith("The draft's action 3 is typed text under <[ Find Email ]>, and the answer has a single click with the left button under <[ Find Submit ]> there.");
        }

        [Fact]
        public void The_last_actions_left_out_are_refused()
        {
            FlowScriptSchema answer = new FlowScriptSchema();
            Click(answer, Search(answer, null, "Find Login"));

            FlowStep email = Search(answer, null, "Find Email");
            Click(answer, email);
            Type(answer, email);

            Refusal(answer).ShouldStartWith("The answer leaves out the draft's action 4, a single click with the left button under <[ Find Submit ]>, and every one after it.");
        }

        [Fact]
        public void An_action_added_is_refused()
        {
            FlowScriptSchema answer = Draft();
            Press(answer, null, "Escape");

            Refusal(answer).ShouldStartWith("The answer adds the key Escape pressed after the draft's last action.");
        }

        [Fact]
        public void Actions_in_another_order_are_refused()
        {
            FlowScriptSchema answer = new FlowScriptSchema();
            Click(answer, Search(answer, null, "Find Login"));

            FlowStep email = Search(answer, null, "Find Email");
            Type(answer, email);
            Click(answer, email);

            FlowStep submit = Search(answer, null, "Find Submit");
            Click(answer, submit);
            Press(answer, submit, "Enter");

            Refusal(answer).ShouldStartWith("The draft's action 2 is a single click with the left button");
        }

        [Fact]
        public void Clicks_folded_into_a_Find_All_Images_count_as_the_clicks_they_replaced()
        {
            FlowScriptSchema draft = new FlowScriptSchema();
            for (int row = 1; row <= 3; row++)
                Click(draft, Search(draft, null, $"Find Download {row}"));
            Click(draft, Search(draft, null, "Find Log out"));

            FlowScriptSchema answer = new FlowScriptSchema();
            Click(answer, Search(answer, null, "Find every download", SearchModeEnum.FIND_ALL));
            Click(answer, Search(answer, null, "Find Log out"));

            RecordedActionsCheck.Check(draft, answer).ShouldBeEmpty();
        }

        [Fact]
        public void A_Find_All_Images_does_at_least_once_what_the_draft_did()
        {
            FlowScriptSchema draft = new FlowScriptSchema();
            Click(draft, Search(draft, null, "Find Download"));

            FlowScriptSchema answer = new FlowScriptSchema();
            Type(answer, Search(answer, null, "Find every download", SearchModeEnum.FIND_ALL));

            RecordedActionsCheck.Check(draft, answer).ShouldHaveSingleItem().Message.ShouldStartWith("The draft's action 1 is a single click with the left button");
        }

        // An execution that finds everything never goes there.
        [Fact]
        public void What_a_Failure_branch_does_is_not_on_the_way()
        {
            FlowScriptSchema answer = Draft();
            FlowStep failure = answer.Steps.First(x => x.FlowStepType == FlowStepTypeEnum.FAILURE);
            Press(answer, failure, "Escape");

            RecordedActionsCheck.Check(Draft(), answer).ShouldBeEmpty();
        }
    }
}
