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

        private static void Stage(FlowScriptSchema schema, FlowStep? parent, string name)
        {
            Add(schema, parent, new FlowStep { FlowStepType = FlowStepTypeEnum.STAGE_MARKER, Name = name });
        }

        // The draft above with a stage over one of its searches, or at the end of the one before.
        private static FlowScriptSchema Staged(string name, int beforeSearch, bool isInBranch = false)
        {
            FlowScriptSchema schema = new FlowScriptSchema();
            FlowStep login = Search(schema, null, "Find Login");
            Click(schema, login);

            if (beforeSearch == 1)
                Stage(schema, isInBranch ? login : null, name);

            FlowStep email = Search(schema, null, "Find Email");
            Click(schema, email);
            Type(schema, email);

            if (beforeSearch == 2)
                Stage(schema, isInBranch ? email : null, name);

            FlowStep submit = Search(schema, null, "Find Submit");
            Click(schema, submit);
            Press(schema, submit, "Enter");

            return schema;
        }

        private static string Refusal(FlowScriptSchema answer)
        {
            Diagnostic refused = RecordedActionsCheck.Check(Draft(), answer).ShouldHaveSingleItem();
            refused.Code.ShouldBe(DiagnosticCodeEnum.ACTIONS_CHANGED);

            return refused.Message;
        }

        // A disabled click does nothing to the application, however the answer keeps its line.
        [Fact]
        public void Disabling_a_recorded_click_is_dropping_it()
        {
            FlowScriptSchema answer = Draft();
            answer.Steps.First(x => x.FlowStepType == FlowStepTypeEnum.CURSOR_CLICK).IsDisabled = true;

            Refusal(answer).ShouldNotBeEmpty();
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

        // ================================================================
        // The tester's stages
        // ================================================================

        [Fact]
        public void The_tester_s_stage_kept_where_it_was_passes_beside_one_the_model_added()
        {
            FlowScriptSchema answer = Staged("Sign in", 1);
            Stage(answer, null, "Submitted");

            RecordedActionsCheck.Check(Staged("Sign in", 1), answer).ShouldBeEmpty();
        }

        // Only a marker at the top of the flow is a stage.
        [Theory]
        [InlineData("Sign in", 2, false)]
        [InlineData("Signing in", 1, false)]
        [InlineData("Sign in", 1, true)]
        public void The_tester_s_stage_moved_renamed_or_put_in_a_branch_is_refused(string name, int beforeSearch, bool isInBranch)
        {
            Diagnostic refused = RecordedActionsCheck.Check(Staged("Sign in", 1), Staged(name, beforeSearch, isInBranch)).ShouldHaveSingleItem();

            refused.Code.ShouldBe(DiagnosticCodeEnum.ACTIONS_CHANGED);
            refused.Message.ShouldContain("the stage <[ Sign in ]>");
        }

        // ================================================================
        // The tester's notifications
        // ================================================================

        // The draft with a check that tells Discord when it fails, reporting itself, and a message
        // marked instead of a click after it. The check's name is the model's to change.
        private static FlowScriptSchema Notifying(string checkName, string failureMessage, string bot, bool isInSuccess = false)
        {
            FlowScriptSchema schema = new FlowScriptSchema();
            FlowStep success = Search(schema, null, checkName);
            FlowStep check = success.ParentFlowStep!;
            FlowStep failure = schema.Steps.Single(x => x.ParentFlowStep == check && x.FlowStepType == FlowStepTypeEnum.FAILURE);

            FlowStep reporting = new FlowStep { FlowStepType = FlowStepTypeEnum.NOTIFY, Message = failureMessage, FlowStepReference = check };
            Add(schema, isInSuccess ? success : failure, reporting);
            schema.DiscordBots[reporting] = new DiscordBotReference { Name = bot };

            FlowStep marked = Add(schema, success, new FlowStep { FlowStepType = FlowStepTypeEnum.NOTIFY, Message = "Order placed" });
            schema.DiscordBots[marked] = new DiscordBotReference { Name = "Alerts" };

            return schema;
        }

        [Fact]
        public void The_tester_s_notifications_kept_with_the_check_renamed_pass_beside_one_the_model_added()
        {
            FlowScriptSchema answer = Notifying("Check the cart is empty", "Remove never showed", "Alerts");
            Add(answer, null, new FlowStep { FlowStepType = FlowStepTypeEnum.NOTIFY, Message = "the model's own" });

            RecordedActionsCheck.Check(Notifying("Check Remove", "Remove never showed", "Alerts"), answer).ShouldBeEmpty();
        }

        [Theory]
        [InlineData("Remove did not show", "Alerts", false)]
        [InlineData("Remove never showed", "Night shift", false)]
        [InlineData("Remove never showed", "Alerts", true)]
        public void The_tester_s_notification_changed_sent_elsewhere_or_moved_to_the_other_branch_is_refused(string message, string bot, bool isInSuccess)
        {
            FlowScriptSchema draft = Notifying("Check Remove", "Remove never showed", "Alerts");

            Diagnostic refused = RecordedActionsCheck.Check(draft, Notifying("Check Remove", message, bot, isInSuccess)).ShouldHaveSingleItem();

            refused.Code.ShouldBe(DiagnosticCodeEnum.ACTIONS_CHANGED);
            refused.Message.ShouldStartWith("The answer drops or changes the draft's Notify <[ Remove never showed ]> through <[ Alerts ]> when the check it reports fails.");
        }

        [Fact]
        public void A_notification_marked_instead_of_a_click_dropped_is_refused()
        {
            FlowScriptSchema answer = Notifying("Check Remove", "Remove never showed", "Alerts");
            answer.Steps.Remove(answer.Steps.Single(x => x.Message == "Order placed"));

            Diagnostic refused = RecordedActionsCheck.Check(Notifying("Check Remove", "Remove never showed", "Alerts"), answer).ShouldHaveSingleItem();

            refused.Message.ShouldStartWith("The answer drops or changes the draft's Notify <[ Order placed ]> through <[ Alerts ]>.");
        }
    }
}
