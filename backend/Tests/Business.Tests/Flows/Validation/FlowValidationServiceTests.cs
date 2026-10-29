using Business.Flows.FlowValidationService;
using Core.Enums;
using Core.Models.Database;
using Core.Models.Dtos;

namespace Business.Tests.Flows.Validation
{
    public sealed class FlowValidationServiceTests
    {
        private readonly List<FlowStep> _steps = new List<FlowStep>();
        private readonly Dictionary<int, int> _templateCounts = new Dictionary<int, int>();
        private readonly List<FlowArea> _areas = new List<FlowArea>();
        private readonly List<FlowPoint> _points = new List<FlowPoint>();
        private readonly List<string> _inputs = new List<string>();
        private bool _isQaTest;

        private FlowStep Add(FlowStepTypeEnum type, string name, FlowStep? parent = null)
        {
            FlowStep step = new FlowStep { Id = _steps.Count + 1, FlowStepType = type, Name = name, ParentFlowStepId = parent?.Id, OrderNumber = _steps.Count };
            _steps.Add(step);
            return step;
        }

        // A search with everything it needs, so a test only sees the problem it made.
        private FlowStep Search(string name, FlowStep? parent = null)
        {
            FlowStep search = Add(FlowStepTypeEnum.SEARCH_IMAGE, name, parent);
            search.FlowAreaId = 100;
            _templateCounts[search.Id] = 1;
            _areas.Add(new FlowArea { Id = 100, Name = "Area " + name, Type = FlowAreaTypeEnum.MONITOR });
            return search;
        }

        private FlowStep Branch(FlowStep parent, FlowStepTypeEnum type)
        {
            return Add(type, string.Empty, parent);
        }

        private FlowValidationResultDto Validate()
        {
            List<string> names = _areas.Select(x => x.Name).Concat(_points.Select(x => x.Name)).Concat(_inputs).ToList();
            return new FlowValidationService().Validate(_steps, _templateCounts, _areas.DistinctBy(x => x.Id).ToList(), _points, names, _isQaTest);
        }

        private List<FlowValidationCodeEnum> CodesOn(FlowStep step)
        {
            return Validate().Issues.Where(x => x.FlowStepId == step.Id).Select(x => x.Code).ToList();
        }

        // ================================================================
        // One step at a time
        // ================================================================

        [Fact]
        public void An_empty_flow_is_an_error()
        {
            FlowValidationResultDto result = Validate();

            result.HasErrors.ShouldBeTrue();
            result.Issues.Single().Code.ShouldBe(FlowValidationCodeEnum.FLOW_HAS_NO_STEPS);
        }

        [Fact]
        public void An_image_search_needs_an_area_and_a_template()
        {
            FlowStep search = Add(FlowStepTypeEnum.SEARCH_IMAGE, "Find");

            List<FlowValidationCodeEnum> codes = CodesOn(search);

            codes.ShouldContain(FlowValidationCodeEnum.AREA_MISSING);
            codes.ShouldContain(FlowValidationCodeEnum.NO_TEMPLATES);
        }

        // A script does not carry the language, so a text search from a file or a recording has
        // none. It reads in the machine's own languages, and executes.
        [Fact]
        public void A_text_search_with_no_language_is_a_warning_not_an_error()
        {
            FlowStep read = Add(FlowStepTypeEnum.SEARCH_TEXT, "Find Invoices");
            read.FlowAreaId = 100;
            read.ConditionType = ConditionTypeEnum.CONTAINS;
            read.ConditionText = "Invoices";
            _areas.Add(new FlowArea { Id = 100, Name = "Invoices area", Type = FlowAreaTypeEnum.MONITOR });

            FlowValidationResultDto result = Validate();

            result.Issues.Single(x => x.Code == FlowValidationCodeEnum.OCR_LANGUAGE_MISSING).Severity.ShouldBe(ValidationSeverityEnum.WARNING);
            result.HasErrors.ShouldBeFalse();
        }

        [Fact]
        public void A_window_step_needs_an_area_and_that_area_a_window()
        {
            _areas.Add(new FlowArea { Id = 1, Name = "Browser", Type = FlowAreaTypeEnum.APPLICATION });
            _areas.Add(new FlowArea { Id = 2, Name = "Screen", Type = FlowAreaTypeEnum.MONITOR });
            _areas.Add(new FlowArea { Id = 3, Name = "Cart", Type = FlowAreaTypeEnum.BROWSER_TAB });

            FlowStep focus = Add(FlowStepTypeEnum.WINDOW_FOCUS, "Focus");
            focus.FlowAreaId = 1;
            FlowStep nowhere = Add(FlowStepTypeEnum.WINDOW_FOCUS, "Focus nothing");
            FlowStep monitor = Add(FlowStepTypeEnum.WINDOW_FOCUS, "Focus a monitor");
            monitor.FlowAreaId = 2;
            FlowStep tab = Add(FlowStepTypeEnum.WINDOW_FOCUS, "Focus a tab");
            tab.FlowAreaId = 3;

            // A window step has Success and Failure, so empty ones are warned about too.
            List<FlowValidationCodeEnum> ErrorsOn(FlowStep step)
            {
                return CodesOn(step).Where(x => x != FlowValidationCodeEnum.BRANCHES_EMPTY).ToList();
            }

            ErrorsOn(focus).ShouldBeEmpty();
            ErrorsOn(nowhere).ShouldBe([FlowValidationCodeEnum.AREA_MISSING]);
            ErrorsOn(monitor).ShouldBe([FlowValidationCodeEnum.AREA_NOT_A_WINDOW]);
            ErrorsOn(tab).ShouldBe([FlowValidationCodeEnum.AREA_NOT_A_WINDOW]);
        }

        // The script writes text between <[ and ]>, so text holding either one could not be exported.
        [Theory]
        [InlineData("Login <[ failed")]
        [InlineData("Login ]> failed")]
        public void Text_holding_a_script_quote_is_an_error(string message)
        {
            FlowStep notify = Add(FlowStepTypeEnum.NOTIFY, "Tell");
            notify.Message = message;

            CodesOn(notify).ShouldContain(FlowValidationCodeEnum.QUOTE_INSIDE);
        }

        // The fields are checked together, but the end of one and the start of the next are not a quote.
        [Fact]
        public void Two_fields_that_meet_at_a_quote_are_not_one()
        {
            FlowStep notify = Add(FlowStepTypeEnum.NOTIFY, "Tell <");
            notify.Message = "[ done";

            CodesOn(notify).ShouldNotContain(FlowValidationCodeEnum.QUOTE_INSIDE);
        }

        [Theory]
        [InlineData(ConditionTypeEnum.EQUALS, "", "", FlowValidationCodeEnum.CONDITION_VALUE_MISSING)]
        [InlineData(ConditionTypeEnum.BETWEEN, "1", "", FlowValidationCodeEnum.CONDITION_RANGE_INCOMPLETE)]
        public void A_condition_needs_what_it_compares_against(ConditionTypeEnum condition, string text, string textEnd, FlowValidationCodeEnum code)
        {
            FlowStep search = Search("Read");
            FlowStep check = Add(FlowStepTypeEnum.CHECK_VALUE, "Check", Branch(search, FlowStepTypeEnum.SUCCESS));
            check.FlowStepReferenceId = search.Id;
            check.ConditionType = condition;
            check.ConditionText = text;
            check.ConditionTextEnd = textEnd;

            CodesOn(check).ShouldContain(code);
        }

        [Fact]
        public void An_empty_condition_needs_nothing_to_compare_against()
        {
            FlowStep search = Search("Read");
            FlowStep check = Add(FlowStepTypeEnum.CHECK_VALUE, "Check", Branch(search, FlowStepTypeEnum.SUCCESS));
            check.FlowStepReferenceId = search.Id;
            check.ConditionType = ConditionTypeEnum.IS_EMPTY;

            CodesOn(check).ShouldNotContain(FlowValidationCodeEnum.CONDITION_VALUE_MISSING);
        }

        // ================================================================
        // Steps that read other steps
        // ================================================================

        [Fact]
        public void A_move_needs_a_point_or_a_result()
        {
            FlowStep move = Add(FlowStepTypeEnum.CURSOR_RELOCATE, "Move");

            CodesOn(move).ShouldContain(FlowValidationCodeEnum.POINT_MISSING);
        }

        // A click and a scroll act where the cursor is; the move above them says where.
        [Theory]
        [InlineData(FlowStepTypeEnum.CURSOR_CLICK)]
        [InlineData(FlowStepTypeEnum.CURSOR_SCROLL)]
        public void A_click_or_a_scroll_needs_no_point(FlowStepTypeEnum type)
        {
            FlowStep step = Add(type, "Act");
            step.LoopCount = 1;

            CodesOn(step).ShouldNotContain(FlowValidationCodeEnum.POINT_MISSING);
        }

        [Fact]
        public void A_move_can_read_a_search_it_sits_under_on_the_success_side()
        {
            FlowStep search = Search("Find");
            FlowStep move = Add(FlowStepTypeEnum.CURSOR_RELOCATE, "Move", Branch(search, FlowStepTypeEnum.SUCCESS));
            move.FlowStepReferenceId = search.Id;

            CodesOn(move).ShouldBeEmpty();
        }

        [Fact]
        public void A_move_cannot_read_a_search_from_its_failure_side()
        {
            FlowStep search = Search("Find");
            FlowStep move = Add(FlowStepTypeEnum.CURSOR_RELOCATE, "Move", Branch(search, FlowStepTypeEnum.FAILURE));
            move.FlowStepReferenceId = search.Id;

            CodesOn(move).ShouldContain(FlowValidationCodeEnum.STEP_RESULT_UNREACHABLE);
        }

        // Waiting until it finds nothing is only a condition, so there is no location to move to.
        [Fact]
        public void A_move_cannot_read_a_search_that_waits_until_it_finds_nothing()
        {
            FlowStep search = Search("Spinner gone");
            search.SearchMode = SearchModeEnum.WAIT_UNTIL_NOT_FOUND;
            FlowStep move = Add(FlowStepTypeEnum.CURSOR_RELOCATE, "Move", Branch(search, FlowStepTypeEnum.SUCCESS));
            move.FlowStepReferenceId = search.Id;

            CodesOn(move).ShouldBe([FlowValidationCodeEnum.STEP_RESULT_NOT_GIVEN]);
        }

        [Fact]
        public void A_value_check_cannot_read_text_that_waits_until_it_stops_matching()
        {
            FlowStep read = Add(FlowStepTypeEnum.SEARCH_TEXT, "Loading gone");
            read.SearchMode = SearchModeEnum.WAIT_UNTIL_NOT_FOUND;
            FlowStep check = Add(FlowStepTypeEnum.CHECK_VALUE, "Check", Branch(read, FlowStepTypeEnum.SUCCESS));
            check.FlowStepReferenceId = read.Id;

            CodesOn(check).ShouldContain(FlowValidationCodeEnum.STEP_RESULT_NOT_GIVEN);
        }

        [Fact]
        public void An_end_execution_under_another_can_never_decide()
        {
            FlowStep first = Add(FlowStepTypeEnum.END_EXECUTION, "Stop");
            FlowStep second = Add(FlowStepTypeEnum.END_EXECUTION, "Stop again", first);

            CodesOn(second).ShouldContain(FlowValidationCodeEnum.END_EXECUTION_UNREACHABLE);
            CodesOn(first).ShouldNotContain(FlowValidationCodeEnum.END_EXECUTION_UNREACHABLE);
        }

        [Fact]
        public void A_notify_can_report_a_search_it_sits_under_on_the_failure_side()
        {
            FlowStep search = Search("Find");
            FlowStep notify = Add(FlowStepTypeEnum.NOTIFY, "Tell", Branch(search, FlowStepTypeEnum.FAILURE));
            notify.FlowStepReferenceId = search.Id;

            CodesOn(notify).ShouldNotContain(FlowValidationCodeEnum.FAILED_STEP_UNREACHABLE);
        }

        [Fact]
        public void A_notify_cannot_report_a_search_from_its_success_side()
        {
            FlowStep search = Search("Find");
            FlowStep notify = Add(FlowStepTypeEnum.NOTIFY, "Tell", Branch(search, FlowStepTypeEnum.SUCCESS));
            notify.FlowStepReferenceId = search.Id;

            CodesOn(notify).ShouldContain(FlowValidationCodeEnum.FAILED_STEP_UNREACHABLE);
        }

        [Fact]
        public void A_break_needs_a_target()
        {
            FlowStep leave = Add(FlowStepTypeEnum.BREAK, "Break");

            CodesOn(leave).ShouldContain(FlowValidationCodeEnum.BREAK_TARGET_MISSING);
        }

        [Fact]
        public void A_break_can_leave_the_check_it_sits_under()
        {
            FlowStep search = Search("Find");
            FlowStep leave = Add(FlowStepTypeEnum.BREAK, "Break", Branch(search, FlowStepTypeEnum.SUCCESS));
            leave.FlowStepReferenceId = search.Id;

            CodesOn(leave).ShouldBeEmpty();
        }

        [Fact]
        public void A_break_can_leave_a_loop_further_up()
        {
            FlowStep loop = Add(FlowStepTypeEnum.LOOP, "Retry");
            loop.LoopCount = 3;
            FlowStep search = Search("Find", loop);
            FlowStep leave = Add(FlowStepTypeEnum.BREAK, "Break", Branch(search, FlowStepTypeEnum.SUCCESS));
            leave.FlowStepReferenceId = loop.Id;

            CodesOn(leave).ShouldBeEmpty();
        }

        [Fact]
        public void A_break_cannot_leave_a_step_beside_it()
        {
            FlowStep wait = Add(FlowStepTypeEnum.WAIT, "Wait");
            wait.WaitForMilliseconds = 100;
            FlowStep leave = Add(FlowStepTypeEnum.BREAK, "Break");
            leave.FlowStepReferenceId = wait.Id;

            CodesOn(leave).ShouldContain(FlowValidationCodeEnum.BREAK_TARGET_UNREACHABLE);
        }

        // Leaving a branch is leaving its check, so the check is what is named.
        [Fact]
        public void A_break_cannot_name_a_success_or_failure_row()
        {
            FlowStep search = Search("Find");
            FlowStep success = Branch(search, FlowStepTypeEnum.SUCCESS);
            FlowStep leave = Add(FlowStepTypeEnum.BREAK, "Break", success);
            leave.FlowStepReferenceId = success.Id;

            CodesOn(leave).ShouldContain(FlowValidationCodeEnum.BREAK_TARGET_UNREACHABLE);
        }

        // ================================================================
        // Names
        // ================================================================

        [Fact]
        public void A_variable_nothing_defines_is_an_error()
        {
            FlowStep type = Add(FlowStepTypeEnum.KEYBOARD_INPUT, "Type");
            type.KeyboardInputText = "{{password}}";

            CodesOn(type).ShouldContain(FlowValidationCodeEnum.VARIABLE_UNKNOWN);
        }

        [Fact]
        public void A_variable_is_known_from_an_input_a_step_above_or_the_viewport()
        {
            _inputs.Add("username");
            FlowStep read = Add(FlowStepTypeEnum.SEARCH_TEXT, "Read the total");
            FlowStep type = Add(FlowStepTypeEnum.KEYBOARD_INPUT, "Type", Branch(read, FlowStepTypeEnum.SUCCESS));
            type.KeyboardInputText = "{{username}} {{Read the total}} {{width}}x{{height}}";

            CodesOn(type).ShouldBeEmpty();
        }

        // What a step read is kept whichever way it went, and only for the steps under it.
        [Fact]
        public void A_steps_value_is_read_on_either_side_of_it_and_nowhere_else()
        {
            FlowStep read = Add(FlowStepTypeEnum.SEARCH_TEXT, "Read the total");
            FlowStep onSuccess = Add(FlowStepTypeEnum.KEYBOARD_INPUT, "Type", Branch(read, FlowStepTypeEnum.SUCCESS));
            onSuccess.KeyboardInputText = "{{Read the total}}";
            FlowStep onFailure = Add(FlowStepTypeEnum.NOTIFY, "Tell", Branch(read, FlowStepTypeEnum.FAILURE));
            onFailure.Message = "The total was {{Read the total}}";
            onFailure.DiscordBotId = 1;
            FlowStep after = Add(FlowStepTypeEnum.END_EXECUTION, "End");
            after.Message = "The total was {{Read the total}}";

            CodesOn(onSuccess).ShouldBeEmpty();
            CodesOn(onFailure).ShouldBeEmpty();
            CodesOn(after).ShouldBe([FlowValidationCodeEnum.STEP_RESULT_UNREACHABLE]);
        }

        [Fact]
        public void A_variable_of_a_step_that_keeps_no_value_is_an_error()
        {
            FlowStep find = Search("Find");
            FlowStep type = Add(FlowStepTypeEnum.KEYBOARD_INPUT, "Type", Branch(find, FlowStepTypeEnum.SUCCESS));
            type.KeyboardInputText = "{{Find}}";

            FlowStep gone = Add(FlowStepTypeEnum.SEARCH_TEXT, "Banner gone");
            gone.SearchMode = SearchModeEnum.WAIT_UNTIL_NOT_FOUND;
            FlowStep tell = Add(FlowStepTypeEnum.NOTIFY, "Tell", Branch(gone, FlowStepTypeEnum.SUCCESS));
            tell.Message = "{{Banner gone}}";
            tell.DiscordBotId = 1;

            CodesOn(type).ShouldBe([FlowValidationCodeEnum.STEP_RESULT_NOT_GIVEN]);
            CodesOn(tell).ShouldBe([FlowValidationCodeEnum.STEP_RESULT_NOT_GIVEN]);
        }

        [Fact]
        public void A_step_named_like_an_area_is_a_duplicate()
        {
            FlowStep step = Add(FlowStepTypeEnum.WAIT, "Browser");
            _areas.Add(new FlowArea { Id = 7, Name = "browser" });

            CodesOn(step).ShouldContain(FlowValidationCodeEnum.NAME_DUPLICATE);
        }

        [Fact]
        public void An_area_and_a_point_with_one_name_are_a_duplicate_with_no_step_to_blame()
        {
            Add(FlowStepTypeEnum.WAIT, "Wait");
            _areas.Add(new FlowArea { Id = 7, Name = "Header" });
            _points.Add(new FlowPoint { Id = 8, Name = "Header" });

            Validate().Issues.ShouldContain(x => x.Code == FlowValidationCodeEnum.NAME_DUPLICATE && x.FlowStepId == null);
        }

        // ================================================================
        // Warnings
        // ================================================================

        [Fact]
        public void A_warning_alone_is_not_an_error()
        {
            Search("Find");

            FlowValidationResultDto result = Validate();

            result.Issues.ShouldNotBeEmpty();
            result.HasErrors.ShouldBeFalse();
        }

        // ================================================================
        // Screen coordinates
        // ================================================================

        [Fact]
        public void Every_step_using_something_in_screen_coordinates_is_warned_and_nothing_else()
        {
            _areas.Add(new FlowArea { Id = 1, Name = "Screen", Type = FlowAreaTypeEnum.MONITOR });
            _areas.Add(new FlowArea { Id = 2, Name = "Drawn box", Type = FlowAreaTypeEnum.CUSTOM });
            _areas.Add(new FlowArea { Id = 3, Name = "Inside the box", Type = FlowAreaTypeEnum.CUSTOM, ParentFlowAreaId = 2 });
            _areas.Add(new FlowArea { Id = 4, Name = "Inside the screen", Type = FlowAreaTypeEnum.CUSTOM, ParentFlowAreaId = 1 });
            _points.Add(new FlowPoint { Id = 10, Name = "Loose point" });
            _points.Add(new FlowPoint { Id = 11, Name = "Anchored point", FlowAreaId = 1 });
            _points.Add(new FlowPoint { Id = 12, Name = "Point in the box", FlowAreaId = 2 });

            FlowStep box = Add(FlowStepTypeEnum.SEARCH_TEXT, "Search the box");
            box.FlowAreaId = 2;
            FlowStep insideBox = Add(FlowStepTypeEnum.SEARCH_TEXT, "Search inside the box");
            insideBox.FlowAreaId = 3;
            FlowStep insideScreen = Add(FlowStepTypeEnum.SEARCH_TEXT, "Search inside the screen");
            insideScreen.FlowAreaId = 4;
            FlowStep drag = Add(FlowStepTypeEnum.CURSOR_DRAG, "Drag");
            drag.FlowPointId = 10;
            drag.FlowPointEndId = 11;
            FlowStep move = Add(FlowStepTypeEnum.CURSOR_RELOCATE, "Move");
            move.FlowPointId = 12;

            List<int?> warned = Validate().Issues
                .Where(x => x.Code == FlowValidationCodeEnum.SCREEN_COORDINATES)
                .Select(x => x.FlowStepId)
                .ToList();

            warned.ShouldBe([box.Id, insideBox.Id, drag.Id, move.Id], ignoreOrder: true);
        }

        // ================================================================
        // Held keys
        // ================================================================

        private FlowStep Keys(string name, string keys, KeyboardKeyActionTypeEnum keyAction, FlowStep? parent = null)
        {
            FlowStep step = Add(FlowStepTypeEnum.KEYBOARD_INPUT, name, parent);
            step.KeyboardInputType = KeyboardInputTypeEnum.COMBINATION;
            step.KeyboardInputText = keys;
            step.KeyboardKeyActionType = keyAction;
            return step;
        }

        [Fact]
        public void Keys_held_with_nothing_letting_them_go_are_a_warning()
        {
            FlowStep hold = Keys("Hold", "Ctrl", KeyboardKeyActionTypeEnum.HOLD);

            CodesOn(hold).ShouldBe([FlowValidationCodeEnum.KEYS_NOT_RELEASED]);
        }

        // Written the other way round, in other case, it lets go of the same keys.
        [Fact]
        public void Keys_let_go_of_further_down_are_fine()
        {
            FlowStep hold = Keys("Hold", "Ctrl+Shift", KeyboardKeyActionTypeEnum.HOLD);
            FlowStep search = Search("Find");
            FlowStep success = Branch(search, FlowStepTypeEnum.SUCCESS);
            Keys("Release", "shift+ctrl", KeyboardKeyActionTypeEnum.RELEASE, success);

            CodesOn(hold).ShouldBeEmpty();
        }

        [Fact]
        public void A_release_above_the_hold_or_of_other_keys_does_not_count()
        {
            Keys("Release first", "Ctrl", KeyboardKeyActionTypeEnum.RELEASE);
            FlowStep hold = Keys("Hold", "Ctrl", KeyboardKeyActionTypeEnum.HOLD);
            Keys("Release Shift", "Shift", KeyboardKeyActionTypeEnum.RELEASE);

            CodesOn(hold).ShouldBe([FlowValidationCodeEnum.KEYS_NOT_RELEASED]);
        }

        // ================================================================
        // A QA test's checks
        // ================================================================

        // A search whose Failure branch only carries on, as one with its End Execution removed does.
        private FlowStep CheckThatCarriesOn(string name, FlowStep? parent = null)
        {
            FlowStep search = Search(name, parent);
            Add(FlowStepTypeEnum.WAIT, "Wait", Branch(search, FlowStepTypeEnum.FAILURE));

            return search;
        }

        [Fact]
        public void A_QA_test_s_check_whose_failure_ends_nothing_is_a_note_that_blocks_nothing()
        {
            _isQaTest = true;
            FlowStep check = CheckThatCarriesOn("Find Saved");

            FlowValidationResultDto result = Validate();

            FlowValidationIssueDto note = result.Issues.Single(x => x.Code == FlowValidationCodeEnum.CHECK_CANNOT_FAIL);
            (note.FlowStepId, note.Severity).ShouldBe((check.Id, ValidationSeverityEnum.INFO));
            result.HasErrors.ShouldBeFalse();
        }

        // Under a check in its Failure branch, whichever way that one goes, is still where its failure ends up.
        [Fact]
        public void A_check_whose_failure_ends_the_execution_however_deep_has_no_note()
        {
            _isQaTest = true;
            FlowStep first = Search("Find Email");
            Add(FlowStepTypeEnum.END_EXECUTION, "Stop", Branch(first, FlowStepTypeEnum.FAILURE));
            FlowStep second = Search("Find Saved");
            FlowStep retry = CheckThatCarriesOn("Find Retry", Branch(second, FlowStepTypeEnum.FAILURE));
            Add(FlowStepTypeEnum.END_EXECUTION, "Stop again", Branch(retry, FlowStepTypeEnum.SUCCESS));

            Validate().Issues.Where(x => x.Code == FlowValidationCodeEnum.CHECK_CANNOT_FAIL).Select(x => x.FlowStepId).ShouldBe([retry.Id]);
        }

        // An automation's search may only decide which way it goes, and the verdict is decided by
        // the time the steps under an End Execution execute.
        [Fact]
        public void An_automation_s_checks_and_those_under_an_end_execution_have_no_note()
        {
            CheckThatCarriesOn("Find Saved");
            Validate().Issues.ShouldNotContain(x => x.Code == FlowValidationCodeEnum.CHECK_CANNOT_FAIL);

            _steps.Clear();
            _isQaTest = true;
            FlowStep first = Search("Find Email");
            FlowStep stop = Add(FlowStepTypeEnum.END_EXECUTION, "Stop", Branch(first, FlowStepTypeEnum.FAILURE));
            CheckThatCarriesOn("Find Dialog", stop);
            Validate().Issues.ShouldNotContain(x => x.Code == FlowValidationCodeEnum.CHECK_CANNOT_FAIL);
        }
    }
}
