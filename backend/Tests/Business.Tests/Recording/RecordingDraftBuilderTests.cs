using System.Drawing;
using Business.FlowScript.Models;
using Business.FlowScript.Scanner;
using Business.FlowScript.Text;
using Business.Recording.Draft;
using Business.Tests.Fakes;
using Core.Enums;
using Core.Models.Database;
using Core.Models.Dtos;

namespace Business.Tests.Recording
{
    /// <summary>
    /// A reviewed recording into a flow, with no model and no questions. The main area was at
    /// 100, 50 and 800 by 600, at 120 dpi, for every press.
    /// </summary>
    public sealed class RecordingDraftBuilderTests
    {
        private static readonly Rectangle MAIN = new Rectangle(100, 50, 800, 600);

        private readonly FakeScreenshotService _screenshots = new FakeScreenshotService();
        private readonly RecordingDraftBuilder _builder;
        private readonly RecordingResultDto _recording = new RecordingResultDto();
        private readonly ReviewedRecordingDto _reviewed = new ReviewedRecordingDto();
        private readonly List<int> _pressed = new List<int>();

        public RecordingDraftBuilderTests()
        {
            _builder = new RecordingDraftBuilder(_screenshots);
        }

        private static FlowScriptSchema Flow()
        {
            FlowArea browser = new FlowArea { Name = "Browser", IsMain = true, Type = FlowAreaTypeEnum.APPLICATION, ProcessName = "chrome.exe", ScalesWith = ScalesWithEnum.DPI };
            FlowStep launch = new FlowStep { Name = "Launch Browser", FlowStepType = FlowStepTypeEnum.SYSTEM_COMMAND, RunCommandPreset = RunCommandPresetEnum.LAUNCH_APP, RunCommandValue = "chrome.exe", FlowArea = browser };

            FlowScriptSchema schema = new FlowScriptSchema { Flow = new Flow { Name = "Download invoices", PublicId = Guid.Parse("8f14e45f-ea2b-4c3f-9f1a-77f0d2a3b111") } };
            schema.Areas.Add(browser);
            schema.Steps.Add(launch);

            return schema;
        }

        private RecordedActionDto Act(RecordedActionDto action)
        {
            action.Index = _recording.Actions.Count;
            _recording.Actions.Add(action);

            return action;
        }

        private void Review(RecordedActionDto action, ReviewChoiceEnum choice, string? inputName = null, bool isSecret = false)
        {
            _reviewed.Actions.Add(new ReviewedActionDto { ActionIndex = action.Index, Choice = choice, InputName = inputName, IsSecret = isSecret });
        }

        // A press inside the main area, with the 40 by 20 element the model found centred on it.
        private RecordedActionDto Click(int x, int y, string text, ReviewChoiceEnum choice = ReviewChoiceEnum.WAIT_AND_CLICK, int waited = 1000, RecordedActionKindEnum kind = RecordedActionKindEnum.CLICK)
        {
            RecordedActionDto action = Act(new RecordedActionDto { Kind = kind, LocationX = x, LocationY = y, WaitedMilliseconds = waited, Window = RecordedWindowEnum.MAIN_AREA });
            action.ScreenshotIndex = action.Index;
            _pressed.Add(action.Index);

            _recording.ClickedElements.Add(new ClickedElementDto
            {
                ActionIndex = action.Index,
                X = x - MAIN.X - 20,
                Y = y - MAIN.Y - 10,
                Width = 40,
                Height = 20,
                Text = text,
                FoundBy = ElementFoundByEnum.MODEL,
                Image = [2],
            });

            Review(action, choice);
            return action;
        }

        private RecordedActionDto Typing(string text, ReviewChoiceEnum choice = ReviewChoiceEnum.INPUT, string? inputName = null, bool isSecret = false)
        {
            RecordedActionDto action = Act(new RecordedActionDto { Kind = RecordedActionKindEnum.TYPING, Text = text });
            Review(action, choice, inputName, isSecret);

            return action;
        }

        private RecordedActionDto Key(RecordedActionKindEnum kind, string key)
        {
            RecordedActionDto action = Act(new RecordedActionDto { Kind = kind, Text = key });
            Review(action, ReviewChoiceEnum.KEEP);

            return action;
        }

        // The session's rule: an action's own press, or the last click's before it.
        private RecordingScreenDto? ScreenAt(int actionIndex)
        {
            List<int> before = _pressed.Where(x => x <= actionIndex).ToList();
            if (before.Count == 0)
                return null;

            return new RecordingScreenDto { ActionIndex = before[^1], Image = [1], X = MAIN.X, Y = MAIN.Y, Width = MAIN.Width, Height = MAIN.Height, Dpi = 120 };
        }

        private FlowScriptSchema Build()
        {
            return _builder.Build(Flow(), _recording, _reviewed, ScreenAt, null, null);
        }

        private static List<FlowStep> ChildrenOf(FlowScriptSchema schema, FlowStep? parent)
        {
            return schema.Steps.Where(x => x.ParentFlowStep == parent).OrderBy(x => x.OrderNumber).ToList();
        }

        // Found by the words Review says it waits to read, inside the rectangle Review left.
        private void ByText(RecordedActionDto action, string words)
        {
            ReviewedActionDto review = _reviewed.Actions.Single(x => x.ActionIndex == action.Index);
            (review.SearchBy, review.SearchText) = (SearchByEnum.TEXT, words);
        }

        // Marked anywhere: where does not matter to a step that is not a click.
        private RecordedActionDto Substitute(ReviewChoiceEnum choice, string? commandText = null, string? inputName = null)
        {
            RecordedActionDto action = Act(new RecordedActionDto { Kind = RecordedActionKindEnum.SUBSTITUTE, LocationX = 400, LocationY = 300, Window = RecordedWindowEnum.MAIN_AREA });
            _reviewed.Actions.Add(new ReviewedActionDto { ActionIndex = action.Index, Choice = choice, CommandText = commandText, InputName = inputName });

            return action;
        }

        private static FlowStep BranchOf(FlowScriptSchema schema, FlowStep search, FlowStepTypeEnum branch)
        {
            return ChildrenOf(schema, search).Single(x => x.FlowStepType == branch);
        }

        // ================================================================
        // A click
        // ================================================================

        [Fact]
        public void A_click_waited_for_is_a_search_then_a_move_onto_it_and_a_click_in_its_success_branch()
        {
            Click(400, 300, "Invoices");

            FlowScriptSchema schema = Build();

            FlowStep search = schema.Steps.Single(x => x.FlowStepType == FlowStepTypeEnum.SEARCH_IMAGE);
            (search.Name, search.SearchMode, search.FlowArea!.Name).ShouldBe(("Find Invoices", SearchModeEnum.WAIT_UNTIL_FOUND, "Browser"));
            search.CodeComment.ShouldBe("recorded after a 1.0s wait");

            List<FlowStep> success = ChildrenOf(schema, BranchOf(schema, search, FlowStepTypeEnum.SUCCESS));
            success.Select(x => x.FlowStepType).ShouldBe([FlowStepTypeEnum.CURSOR_RELOCATE, FlowStepTypeEnum.CURSOR_CLICK]);
            success[0].FlowStepReference.ShouldBeSameAs(search);
        }

        // A flow that carries on after a wait timed out clicks blind.
        [Fact]
        public void A_search_that_finds_nothing_ends_the_execution_failed_and_says_why()
        {
            Click(400, 300, "Invoices");

            FlowScriptSchema schema = Build();

            FlowStep search = schema.Steps.Single(x => x.FlowStepType == FlowStepTypeEnum.SEARCH_IMAGE);
            FlowStep end = ChildrenOf(schema, BranchOf(schema, search, FlowStepTypeEnum.FAILURE)).ShouldHaveSingleItem();
            (end.FlowStepType, end.EndExecutionAsSuccess).ShouldBe((FlowStepTypeEnum.END_EXECUTION, false));
            end.Message.ShouldBe("Find Invoices found nothing within 10.0s");
        }

        [Theory]
        [InlineData(0, 10_000)]
        [InlineData(4_200, 12_600)]
        [InlineData(30_000, 60_000)]
        public void The_wait_is_three_times_what_the_tester_waited_never_under_10_s_nor_over_60_s(int waited, int timeout)
        {
            Click(400, 300, "Invoices", waited: waited);

            Build().Steps.Single(x => x.FlowStepType == FlowStepTypeEnum.SEARCH_IMAGE).TimeoutMilliseconds.ShouldBe(timeout);
        }

        [Fact]
        public void Everything_after_a_search_sits_in_its_success_branch_until_the_next_search()
        {
            Click(300, 200, "Email");
            Typing("ops@example.com", inputName: "email");
            Click(300, 400, "Sign in");

            FlowScriptSchema schema = Build();

            List<FlowStep> searches = ChildrenOf(schema, null).Where(x => x.FlowStepType == FlowStepTypeEnum.SEARCH_IMAGE).ToList();
            searches.Select(x => x.Name).ShouldBe(["Find Email", "Find Sign in"]);

            ChildrenOf(schema, BranchOf(schema, searches[0], FlowStepTypeEnum.SUCCESS)).Select(x => x.FlowStepType)
                .ShouldBe([FlowStepTypeEnum.CURSOR_RELOCATE, FlowStepTypeEnum.CURSOR_CLICK, FlowStepTypeEnum.KEYBOARD_INPUT]);
            ChildrenOf(schema, BranchOf(schema, searches[1], FlowStepTypeEnum.SUCCESS)).Select(x => x.FlowStepType)
                .ShouldBe([FlowStepTypeEnum.CURSOR_RELOCATE, FlowStepTypeEnum.CURSOR_CLICK]);
        }

        [Fact]
        public void The_recording_comes_after_the_steps_the_flow_already_has()
        {
            Click(400, 300, "Invoices");

            ChildrenOf(Build(), null).Select(x => x.FlowStepType).ShouldBe([FlowStepTypeEnum.SYSTEM_COMMAND, FlowStepTypeEnum.SEARCH_IMAGE]);
        }

        // The editor's "Record steps here".
        [Fact]
        public void A_recording_at_a_position_goes_in_between_and_the_steps_after_it_move_down()
        {
            FlowScriptSchema flow = Flow();
            flow.Steps.Add(new FlowStep { Name = "Wait", FlowStepType = FlowStepTypeEnum.WAIT, OrderNumber = 1 });
            Click(400, 300, "Invoices");

            FlowScriptSchema schema = _builder.Build(flow, _recording, _reviewed, ScreenAt, null, 1);

            ChildrenOf(schema, null).Select(x => x.Name).ShouldBe(["Launch Browser", "Find Invoices", "Wait"]);
        }

        // Each search beside the one before it, as at the top: what follows one is in its Success branch.
        [Fact]
        public void A_recording_under_a_branch_puts_its_searches_beside_each_other_there()
        {
            FlowScriptSchema flow = Flow();
            FlowStep check = new FlowStep { Name = "Signed in", FlowStepType = FlowStepTypeEnum.SEARCH_IMAGE, OrderNumber = 1 };
            FlowStep success = new FlowStep { FlowStepType = FlowStepTypeEnum.SUCCESS, ParentFlowStep = check, OrderNumber = 0 };
            flow.Steps.AddRange([check, success, new FlowStep { FlowStepType = FlowStepTypeEnum.FAILURE, ParentFlowStep = check, OrderNumber = 1 }]);
            Click(300, 200, "Email");
            Typing("ops@example.com", inputName: "email");
            Click(300, 400, "Sign in");

            FlowScriptSchema schema = _builder.Build(flow, _recording, _reviewed, ScreenAt, success, 0);

            ChildrenOf(schema, success).Select(x => x.Name).ShouldBe(["Find Email", "Find Sign in"]);
            ChildrenOf(schema, null).Select(x => x.Name).ShouldBe(["Launch Browser", "Signed in"]);
        }

        [Fact]
        public void A_click_at_its_exact_spot_moves_to_a_point_inside_the_main_area()
        {
            Click(400, 300, "Invoices", ReviewChoiceEnum.CLICK_AT_SPOT);

            FlowScriptSchema schema = Build();

            schema.Steps.ShouldNotContain(x => x.FlowStepType == FlowStepTypeEnum.SEARCH_IMAGE);
            FlowPoint point = schema.Points.ShouldHaveSingleItem();
            (point.Name, point.FlowArea!.Name, point.LocationX, point.LocationY, point.AuthoredDpi).ShouldBe(("Click Invoices", "Browser", 300, 250, 120));
            ChildrenOf(schema, null).Single(x => x.FlowStepType == FlowStepTypeEnum.CURSOR_RELOCATE).FlowPoint.ShouldBeSameAs(point);
        }

        // ================================================================
        // Templates
        // ================================================================

        [Fact]
        public void The_template_is_cut_along_the_rectangle_review_left_and_knows_where_the_click_landed()
        {
            RecordedActionDto click = Click(400, 300, "Invoices");
            ReviewedActionDto review = _reviewed.Actions.Single();
            (review.ElementX, review.ElementY, review.ElementWidth, review.ElementHeight) = (270, 240, 60, 30);

            FlowScriptSchema schema = Build();

            _screenshots.Encoded.Select(x => (x.Width, x.Height)).ShouldBe([(60, 30)]);
            FlowTemplate template = schema.Steps.Single(x => x.FlowStepType == FlowStepTypeEnum.SEARCH_IMAGE).FlowStepTemplates.ShouldHaveSingleItem().FlowTemplate;
            (template.ClickOffsetX, template.ClickOffsetY).ShouldBe((click.LocationX - MAIN.X - 270, click.LocationY - MAIN.Y - 240));
            (template.AuthoredFlowAreaWidth, template.AuthoredFlowAreaHeight, template.AuthoredDpi).ShouldBe((800, 600, 120));
            template.Name.ShouldStartWith("template-");
            template.TemplateImage.ShouldNotBeNull();
        }

        [Fact]
        public void Clicks_that_hit_the_same_picture_share_one_template()
        {
            Click(760, 210, "");
            Click(760, 248, "");
            _recording.ClickGroups.Add(new ClickGroupDto { ActionIndexes = [0, 1], IsMoving = true });

            FlowScriptSchema schema = Build();

            // Two links, each with its own accuracy, to one picture.
            List<FlowStepTemplate> links = schema.Steps.SelectMany(x => x.FlowStepTemplates).ToList();
            links.Count.ShouldBe(2);
            links.Select(x => x.FlowTemplate).Distinct().ShouldHaveSingleItem();
            _screenshots.Encoded.Count.ShouldBe(1);
        }

        [Fact]
        public void Steps_are_named_from_what_was_read_and_never_twice()
        {
            Click(300, 200, "Save");
            Click(500, 200, "Save");
            Click(700, 200, "");

            Build().Steps.Where(x => x.FlowStepType == FlowStepTypeEnum.SEARCH_IMAGE).Select(x => x.Name).ShouldBe(["Find Save", "Find Save 2", "Find icon"]);
        }

        // ================================================================
        // Typing and keys
        // ================================================================

        [Fact]
        public void Typed_text_is_an_input_with_what_was_typed_as_its_default_and_a_secret_keeps_none()
        {
            Typing("ops@example.com", inputName: "email");
            Typing("hunter2", inputName: "password", isSecret: true);

            FlowScriptSchema schema = Build();

            schema.Inputs.Select(x => (x.Name, x.DefaultValue, x.IsSecret)).ShouldBe([("email", "ops@example.com", false), ("password", "", true)]);
            schema.Steps.Where(x => x.FlowStepType == FlowStepTypeEnum.KEYBOARD_INPUT).Select(x => x.KeyboardInputText).ShouldBe(["{{email}}", "{{password}}"]);
        }

        [Fact]
        public void Text_typed_as_written_is_on_the_line_itself()
        {
            Typing("Invoices for September", ReviewChoiceEnum.TYPE_AS_WRITTEN);

            FlowScriptSchema schema = Build();

            schema.Inputs.ShouldBeEmpty();
            schema.Steps.Single(x => x.FlowStepType == FlowStepTypeEnum.KEYBOARD_INPUT).KeyboardInputText.ShouldBe("Invoices for September");
        }

        [Fact]
        public void A_key_held_over_a_click_is_held_before_it_and_released_after()
        {
            Key(RecordedActionKindEnum.KEY_HOLD, "Ctrl");
            Click(400, 300, "Row", ReviewChoiceEnum.CLICK_AT_SPOT);
            Key(RecordedActionKindEnum.KEY_RELEASE, "Ctrl");
            Key(RecordedActionKindEnum.KEY_COMBINATION, "Enter");

            List<FlowStep> keys = Build().Steps.Where(x => x.FlowStepType == FlowStepTypeEnum.KEYBOARD_INPUT).ToList();

            keys.Select(x => (x.KeyboardInputText, x.KeyboardKeyActionType)).ShouldBe(
            [
                ("Ctrl", KeyboardKeyActionTypeEnum.HOLD),
                ("Ctrl", KeyboardKeyActionTypeEnum.RELEASE),
                ("Enter", KeyboardKeyActionTypeEnum.PRESS),
            ]);
        }

        // ================================================================
        // Left out
        // ================================================================

        [Fact]
        public void What_was_left_out_and_a_pause_are_no_step()
        {
            Click(400, 300, "Invoices", ReviewChoiceEnum.LEAVE_OUT);
            Act(new RecordedActionDto { Kind = RecordedActionKindEnum.PAUSE, PauseMilliseconds = 3000 });

            FlowScriptSchema schema = Build();

            schema.Steps.Select(x => x.FlowStepType).ShouldBe([FlowStepTypeEnum.SYSTEM_COMMAND]);
            schema.Points.ShouldBeEmpty();
        }

        // ================================================================
        // Found by its words
        // ================================================================

        // A text search succeeds with its area's centre, which is where the move goes.
        [Fact]
        public void A_click_found_by_its_words_reads_them_in_an_area_of_its_own_and_clicks_its_middle()
        {
            ByText(Click(400, 300, "Invoices"), "Invoices");

            FlowScriptSchema schema = Build();

            FlowStep search = schema.Steps.Single(x => x.FlowStepType == FlowStepTypeEnum.SEARCH_TEXT);
            (search.Name, search.SearchMode, search.ConditionType, search.ConditionText).ShouldBe(("Find Invoices", SearchModeEnum.WAIT_UNTIL_FOUND, ConditionTypeEnum.CONTAINS, "Invoices"));
            search.TimeoutMilliseconds.ShouldBe(10_000);

            FlowArea area = search.FlowArea.ShouldNotBeNull();
            (area.Name, area.Type, area.ParentFlowArea!.Name).ShouldBe(("Find Invoices area", FlowAreaTypeEnum.CUSTOM, "Browser"));
            (area.LocationX, area.LocationY, area.Width, area.Height, area.AuthoredDpi).ShouldBe((280, 240, 40, 20, 120));

            List<FlowStep> success = ChildrenOf(schema, BranchOf(schema, search, FlowStepTypeEnum.SUCCESS));
            success[0].FlowStepReference.ShouldBeSameAs(search);
            ChildrenOf(schema, BranchOf(schema, search, FlowStepTypeEnum.FAILURE)).ShouldHaveSingleItem().Message.ShouldBe("Find Invoices found nothing within 10.0s");
        }

        [Fact]
        public void A_check_by_its_words_that_it_goes_waits_until_its_area_no_longer_reads_them()
        {
            ByText(Click(400, 300, "Loading", ReviewChoiceEnum.CHECK_GONE, kind: RecordedActionKindEnum.CHECK), "Loading");

            FlowStep check = Build().Steps.Single(x => x.FlowStepType == FlowStepTypeEnum.SEARCH_TEXT);

            (check.Name, check.SearchMode, check.FlowArea!.Name).ShouldBe(("Check Loading gone", SearchModeEnum.WAIT_UNTIL_NOT_FOUND, "Check Loading gone area"));
        }

        [Fact]
        public void Words_left_empty_go_by_the_picture()
        {
            ByText(Click(400, 300, "Invoices"), "  ");

            FlowScriptSchema schema = Build();

            schema.Steps.ShouldContain(x => x.FlowStepType == FlowStepTypeEnum.SEARCH_IMAGE);
            schema.Steps.ShouldNotContain(x => x.FlowStepType == FlowStepTypeEnum.SEARCH_TEXT);
            schema.Areas.ShouldHaveSingleItem();
        }

        // ================================================================
        // Marked while recording
        // ================================================================

        [Fact]
        public void A_check_is_a_wait_for_the_element_that_fails_the_execution_when_it_never_comes()
        {
            Click(400, 300, "Paid", ReviewChoiceEnum.CHECK_THERE, kind: RecordedActionKindEnum.CHECK);

            FlowScriptSchema schema = Build();

            FlowStep check = schema.Steps.Single(x => x.FlowStepType == FlowStepTypeEnum.SEARCH_IMAGE);
            (check.Name, check.SearchMode, check.FlowArea!.Name).ShouldBe(("Check Paid", SearchModeEnum.WAIT_UNTIL_FOUND, "Browser"));
            check.FlowStepTemplates.ShouldHaveSingleItem().FlowTemplate.TemplateImage.ShouldNotBeNull();
            ChildrenOf(schema, BranchOf(schema, check, FlowStepTypeEnum.SUCCESS)).ShouldBeEmpty();
            ChildrenOf(schema, BranchOf(schema, check, FlowStepTypeEnum.FAILURE)).ShouldHaveSingleItem().Message.ShouldBe("Check Paid found nothing within 10.0s");
        }

        [Fact]
        public void A_check_that_something_goes_waits_until_it_has_gone()
        {
            Click(400, 300, "Loading", ReviewChoiceEnum.CHECK_GONE, kind: RecordedActionKindEnum.CHECK);

            FlowScriptSchema schema = Build();

            FlowStep check = schema.Steps.Single(x => x.FlowStepType == FlowStepTypeEnum.SEARCH_IMAGE);
            (check.Name, check.SearchMode).ShouldBe(("Check Loading gone", SearchModeEnum.WAIT_UNTIL_NOT_FOUND));
            ChildrenOf(schema, BranchOf(schema, check, FlowStepTypeEnum.FAILURE)).ShouldHaveSingleItem().Message.ShouldBe("Check Loading gone: still there after 10.0s");
        }

        [Fact]
        public void What_comes_after_a_check_sits_in_its_success_branch()
        {
            Click(400, 300, "Signed in", ReviewChoiceEnum.CHECK_THERE, kind: RecordedActionKindEnum.CHECK);
            Key(RecordedActionKindEnum.KEY_COMBINATION, "Enter");

            FlowScriptSchema schema = Build();

            FlowStep check = schema.Steps.Single(x => x.FlowStepType == FlowStepTypeEnum.SEARCH_IMAGE);
            ChildrenOf(schema, BranchOf(schema, check, FlowStepTypeEnum.SUCCESS)).ShouldHaveSingleItem().FlowStepType.ShouldBe(FlowStepTypeEnum.KEYBOARD_INPUT);
        }

        [Fact]
        public void A_command_or_an_application_marked_instead_of_a_click_is_a_command_step()
        {
            Substitute(ReviewChoiceEnum.EXECUTE_COMMAND, commandText: "del /q C:\\Exports\\*.csv");
            Substitute(ReviewChoiceEnum.LAUNCH_APP, commandText: "notepad.exe");

            List<FlowStep> added = ChildrenOf(Build(), null).Skip(1).ToList();

            added.Select(x => (x.FlowStepType, x.RunCommandPreset, x.RunCommandValue)).ShouldBe(
            [
                (FlowStepTypeEnum.SYSTEM_COMMAND, RunCommandPresetEnum.CUSTOM, "del /q C:\\Exports\\*.csv"),
                (FlowStepTypeEnum.SYSTEM_COMMAND, RunCommandPresetEnum.LAUNCH_APP, "notepad.exe"),
            ]);
        }

        [Fact]
        public void An_input_s_value_types_the_input_the_recording_made_or_a_new_one_with_no_value()
        {
            Typing("ops@example.com", inputName: "email");
            Substitute(ReviewChoiceEnum.TYPE_INPUT, inputName: "Email");
            Substitute(ReviewChoiceEnum.TYPE_INPUT, inputName: "account");

            FlowScriptSchema schema = Build();

            schema.Steps.Where(x => x.FlowStepType == FlowStepTypeEnum.KEYBOARD_INPUT).Select(x => x.KeyboardInputText).ShouldBe(["{{email}}", "{{email}}", "{{account}}"]);
            schema.Inputs.Select(x => (x.Name, x.DefaultValue)).ShouldBe([("email", "ops@example.com"), ("account", "")]);
        }

        [Fact]
        public void One_left_with_nothing_to_execute_open_or_type_is_no_step()
        {
            Substitute(ReviewChoiceEnum.EXECUTE_COMMAND, commandText: "  ");
            Substitute(ReviewChoiceEnum.LAUNCH_APP);
            Substitute(ReviewChoiceEnum.TYPE_INPUT, inputName: "");

            ChildrenOf(Build(), null).ShouldHaveSingleItem().Name.ShouldBe("Launch Browser");
        }

        // ================================================================
        // As a script
        // ================================================================

        // Settled 2026-10-09: every draft prints and reads back with no diagnostics.
        [Fact]
        public void A_draft_of_every_kind_of_action_prints_and_reads_back_with_no_diagnostics()
        {
            Click(300, 200, "Email");
            Typing("ops@example.com", inputName: "email");
            Key(RecordedActionKindEnum.KEY_COMBINATION, "Tab");
            Typing("hunter2", inputName: "password", isSecret: true);
            Key(RecordedActionKindEnum.KEY_COMBINATION, "Enter");
            Click(150, 120, "Invoices", ReviewChoiceEnum.CLICK_AT_SPOT, waited: 3800);
            Act(new RecordedActionDto { Kind = RecordedActionKindEnum.PAUSE, PauseMilliseconds = 3800 });
            RecordedActionDto scroll = Act(new RecordedActionDto { Kind = RecordedActionKindEnum.SCROLL, LocationX = 500, LocationY = 400, ScrollDirection = CursorScrollDirectionTypeEnum.DOWN, ScrollAmount = 3 });
            Review(scroll, ReviewChoiceEnum.KEEP);
            RecordedActionDto drag = Click(300, 300, "Card", kind: RecordedActionKindEnum.DRAG);
            (drag.LocationEndX, drag.LocationEndY) = (600, 300);
            Key(RecordedActionKindEnum.KEY_HOLD, "Ctrl");
            Click(760, 210, "");
            Key(RecordedActionKindEnum.KEY_RELEASE, "Ctrl");
            Click(760, 248, "");
            _recording.ClickGroups.Add(new ClickGroupDto { ActionIndexes = [10, 12], IsMoving = true });
            Typing("September", ReviewChoiceEnum.TYPE_AS_WRITTEN);
            RecordedActionDto outside = Act(new RecordedActionDto { Kind = RecordedActionKindEnum.CLICK, LocationX = 1500, LocationY = 1060, Window = RecordedWindowEnum.OTHER });
            Review(outside, ReviewChoiceEnum.CLICK_AT_SPOT);
            RecordedActionDto menu = Click(420, 330, "Row", ReviewChoiceEnum.CLICK_AT_SPOT);
            (menu.CursorButtonType, menu.CursorButtonActionType) = (CursorButtonTypeEnum.RIGHT_BUTTON, CursorButtonActionTypeEnum.DOUBLE_CLICK);
            RecordedActionDto slide = Click(200, 500, "Slider", ReviewChoiceEnum.CLICK_AT_SPOT, kind: RecordedActionKindEnum.DRAG);
            (slide.LocationEndX, slide.LocationEndY) = (320, 500);
            Click(400, 300, "Paid", ReviewChoiceEnum.CHECK_THERE, kind: RecordedActionKindEnum.CHECK);
            Click(400, 340, "Loading", ReviewChoiceEnum.CHECK_GONE, kind: RecordedActionKindEnum.CHECK);
            Substitute(ReviewChoiceEnum.EXECUTE_COMMAND, commandText: "del /q C:\\Exports\\*.csv");
            Substitute(ReviewChoiceEnum.LAUNCH_APP, commandText: "notepad.exe");
            Substitute(ReviewChoiceEnum.TYPE_INPUT, inputName: "email");
            ByText(Click(400, 380, "Download"), "Download");
            ByText(Click(400, 420, "Saved", ReviewChoiceEnum.CHECK_THERE, kind: RecordedActionKindEnum.CHECK), "Saved");

            FlowScriptSchema draft = Build();
            string script = new Printer().Write(draft);
            FlowScriptSchema read = new Scanner().Read(script);

            // A check followed straight by a search leaves its Success branch empty, which a script
            // does not write and an import adds back (FlowStepDataService.MissingBranches).
            int emptyBranches = draft.Steps.Count(x => x.FlowStepType == FlowStepTypeEnum.SUCCESS && !draft.Steps.Any(y => y.ParentFlowStep == x));

            read.Diagnostics.ShouldBeEmpty(script);
            emptyBranches.ShouldBe(2, script);     // Check Paid, followed by a check, and Check Saved, the last
            read.Steps.Count.ShouldBe(draft.Steps.Count - emptyBranches, script);
            read.Points.Count.ShouldBe(draft.Points.Count, script);
            read.Inputs.Count.ShouldBe(2, script);
            new Printer().Write(read).ShouldBe(script);
        }
    }
}
