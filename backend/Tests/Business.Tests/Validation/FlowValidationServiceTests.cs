using Business.Validation;
using Core.Enums;
using Core.Models.Database;
using Core.Models.Dtos;

namespace Business.Tests.Validation
{
    public sealed class FlowValidationServiceTests
    {
        private readonly List<FlowStep> _steps = new List<FlowStep>();
        private readonly Dictionary<int, int> _templateCounts = new Dictionary<int, int>();
        private readonly List<FlowArea> _areas = new List<FlowArea>();
        private readonly List<FlowPoint> _points = new List<FlowPoint>();
        private readonly List<string> _inputs = new List<string>();

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
            return new FlowValidationService().Validate(_steps, _templateCounts, _areas.DistinctBy(x => x.Id).ToList(), _points, names);
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
        public void A_click_needs_a_point_or_a_result()
        {
            FlowStep click = Add(FlowStepTypeEnum.CURSOR_CLICK, "Click");

            CodesOn(click).ShouldContain(FlowValidationCodeEnum.POINT_MISSING);
        }

        [Fact]
        public void A_click_can_read_a_search_it_sits_under_on_the_success_side()
        {
            FlowStep search = Search("Find");
            FlowStep click = Add(FlowStepTypeEnum.CURSOR_CLICK, "Click", Branch(search, FlowStepTypeEnum.SUCCESS));
            click.FlowStepReferenceId = search.Id;

            CodesOn(click).ShouldBeEmpty();
        }

        [Fact]
        public void A_click_cannot_read_a_search_from_its_failure_side()
        {
            FlowStep search = Search("Find");
            FlowStep click = Add(FlowStepTypeEnum.CURSOR_CLICK, "Click", Branch(search, FlowStepTypeEnum.FAILURE));
            click.FlowStepReferenceId = search.Id;

            CodesOn(click).ShouldContain(FlowValidationCodeEnum.STEP_RESULT_UNREACHABLE);
        }

        [Fact]
        public void An_end_execution_under_another_can_never_decide()
        {
            FlowStep first = Add(FlowStepTypeEnum.END_EXECUTION, "Stop");
            FlowStep second = Add(FlowStepTypeEnum.END_EXECUTION, "Stop again", first);

            CodesOn(second).ShouldContain(FlowValidationCodeEnum.END_EXECUTION_UNREACHABLE);
            CodesOn(first).ShouldNotContain(FlowValidationCodeEnum.END_EXECUTION_UNREACHABLE);
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
        public void A_variable_is_known_from_an_input_a_step_or_the_viewport()
        {
            _inputs.Add("username");
            Search("Read the total");
            FlowStep type = Add(FlowStepTypeEnum.KEYBOARD_INPUT, "Type");
            type.KeyboardInputText = "{{username}} {{Read the total}} {{width}}x{{height}}";

            CodesOn(type).ShouldNotContain(FlowValidationCodeEnum.VARIABLE_UNKNOWN);
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
        // Checks that decide nothing
        // ================================================================

        [Fact]
        public void A_check_nothing_reads_and_nothing_fails_on_decides_nothing()
        {
            FlowStep search = Search("Find");

            CodesOn(search).ShouldContain(FlowValidationCodeEnum.CHECK_DECIDES_NOTHING);
        }

        [Fact]
        public void A_check_that_ends_the_execution_on_failure_decides_something()
        {
            FlowStep search = Search("Find");
            Add(FlowStepTypeEnum.END_EXECUTION, "Stop", Branch(search, FlowStepTypeEnum.FAILURE));

            CodesOn(search).ShouldNotContain(FlowValidationCodeEnum.CHECK_DECIDES_NOTHING);
        }

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
            FlowStep click = Add(FlowStepTypeEnum.CURSOR_CLICK, "Click");
            click.FlowPointId = 12;

            List<int?> warned = Validate().Issues
                .Where(x => x.Code == FlowValidationCodeEnum.SCREEN_COORDINATES)
                .Select(x => x.FlowStepId)
                .ToList();

            warned.ShouldBe([box.Id, insideBox.Id, drag.Id, click.Id], ignoreOrder: true);
        }
    }
}
