using Business.FlowScript.Catalogs;
using Business.FlowScript.Diagnostics;
using Business.FlowScript.Lexing;
using Business.FlowScript.Models;
using Business.FlowScript.Models.Text;
using Business.FlowScript.Scanner;
using Business.FlowScript.Text;
using Business.FlowScript.Text.Steps;
using Core.Enums;
using Core.Models.Database;

namespace Business.Tests.FlowScript
{
    /// <summary>
    /// Every word the grammar writes, read back. A word that meant one thing on the way out and
    /// another on the way in is a round trip that cannot be relied on.
    /// </summary>
    public sealed class VocabularyTests
    {
        private static FlowScriptSchema Read(string body)
        {
            return new Scanner().Read("Flow: Test\n" + body);
        }

        [Fact]
        public void Every_keyword_is_written_for_its_step_and_read_back_as_it()
        {
            List<string> wrong = new List<string>();

            foreach (ScriptKeyword keyword in ScriptKeywordCatalog.All)
            {
                // The catalogue also holds the smaller vocabularies - a title match, a scroll
                // direction - which are not steps and have their own tests below.
                FlowStepTypeEnum? type = keyword.As<FlowStepTypeEnum>();
                if (type == null)
                    continue;

                FlowStep step = new FlowStep { FlowStepType = type.Value };

                SearchModeEnum? searchMode = keyword.As<SearchModeEnum>();
                if (searchMode != null)
                    step.SearchMode = searchMode.Value;

                KeyboardInputTypeEnum? keyboardInputType = keyword.As<KeyboardInputTypeEnum>();
                if (keyboardInputType != null)
                    step.KeyboardInputType = keyboardInputType.Value;

                RunCommandPresetEnum? runCommandPreset = keyword.As<RunCommandPresetEnum>();
                if (runCommandPreset != null)
                    step.RunCommandPreset = runCommandPreset.Value;

                string line = Printer.StepWriter(step, new FlowScriptSchema()).Write();
                ScriptToken first = ScriptTokenizer.Tokenize(ScriptLineSplitter.Split(line).Single())[0];
                ScriptKeyword? read = ScriptKeywordCatalog.Get<FlowStepTypeEnum>(first.Value);

                if (first.Value != keyword.Text || read != keyword)
                    wrong.Add($"{keyword.Text}: written \"{line}\", read back as \"{read?.Text}\"");
            }

            wrong.ShouldBeEmpty();
        }

        // Keyword throws on a gap, so a value added to one of these enums without a row fails here
        // rather than on somebody's export.
        [Fact]
        public void Every_value_the_printer_writes_has_a_keyword()
        {
            List<Enum> values = new List<Enum>();
            values.AddRange(Enum.GetValues<ScriptSymbolEnum>().Cast<Enum>());
            values.AddRange(Enum.GetValues<ConditionTypeEnum>().Cast<Enum>());
            values.AddRange(Enum.GetValues<TitleMatchModeEnum>().Cast<Enum>());
            values.AddRange(Enum.GetValues<TemplateMatchModeEnum>().Cast<Enum>());
            values.AddRange(Enum.GetValues<ScalesWithEnum>().Cast<Enum>());
            values.AddRange(Enum.GetValues<CursorScrollDirectionTypeEnum>().Cast<Enum>());
            values.AddRange(Enum.GetValues<SystemActionTypeEnum>().Cast<Enum>());

            // A plain left click is never written, and Run and Launch are steps of their own.
            values.AddRange(Enum.GetValues<CursorButtonTypeEnum>().Where(x => x != CursorButtonTypeEnum.LEFT_BUTTON).Cast<Enum>());
            values.AddRange(Enum.GetValues<CursorButtonActionTypeEnum>().Where(x => x != CursorButtonActionTypeEnum.SINGLE_CLICK).Cast<Enum>());
            values.AddRange(Enum.GetValues<RunCommandPresetEnum>().Where(x => x != RunCommandPresetEnum.CUSTOM && x != RunCommandPresetEnum.LAUNCH_APP).Cast<Enum>());

            values.Where(x => ScriptKeywordCatalog.Get(x) == null).ShouldBeEmpty();
        }

        [Fact]
        public void Every_condition_is_written_and_read_back_the_same()
        {
            List<string> wrong = new List<string>();

            foreach (ConditionTypeEnum condition in Enum.GetValues<ConditionTypeEnum>())
            {
                FlowStep step = new FlowStep { FlowStepType = FlowStepTypeEnum.SEARCH_TEXT, Name = "x", ConditionType = condition, ConditionText = @"a ""quoted"" \value\", ConditionTextEnd = "9" };
                string written = new SearchTextWriter(step).Write();
                FlowStep read = Read("Steps:\n" + written).Steps.Single();

                string expectedText = string.Empty;
                if (condition != ConditionTypeEnum.IS_EMPTY && condition != ConditionTypeEnum.IS_NOT_EMPTY)
                    expectedText = step.ConditionText;

                string expectedEnd = string.Empty;
                if (condition == ConditionTypeEnum.BETWEEN)
                    expectedEnd = step.ConditionTextEnd;

                if (read.ConditionType != condition || read.ConditionText != expectedText || read.ConditionTextEnd != expectedEnd)
                    wrong.Add($"{condition}: written \"{written}\", read back as {read.ConditionType} \"{read.ConditionText}\" \"{read.ConditionTextEnd}\"");
            }

            wrong.ShouldBeEmpty();
        }

        [Theory]
        [InlineData(TitleMatchModeEnum.EQUALS)]
        [InlineData(TitleMatchModeEnum.CONTAINS)]
        [InlineData(TitleMatchModeEnum.STARTS_WITH)]
        [InlineData(TitleMatchModeEnum.REGEX)]
        public void A_title_match_is_read_back_as_written(TitleMatchModeEnum mode)
        {
            FlowScriptSchema schema = Read($"Steps:\nFocus Window process <[ p ]> title {ScriptKeywordCatalog.GetTextOfKeyword(mode)} <[ x ]>");

            schema.Steps.Single().TitleMatchMode.ShouldBe(mode);
        }

        [Theory]
        [InlineData(CursorScrollDirectionTypeEnum.UP)]
        [InlineData(CursorScrollDirectionTypeEnum.DOWN)]
        [InlineData(CursorScrollDirectionTypeEnum.LEFT)]
        [InlineData(CursorScrollDirectionTypeEnum.RIGHT)]
        public void A_scroll_direction_is_read_back_as_written(CursorScrollDirectionTypeEnum direction)
        {
            FlowScriptSchema schema = Read($"Steps:\nScroll {ScriptKeywordCatalog.GetTextOfKeyword(direction)} 3");

            schema.Steps.Single().CursorScrollDirectionType.ShouldBe(direction);
        }

        [Fact]
        public void Every_button_and_action_is_read_back_as_written()
        {
            foreach (CursorButtonTypeEnum button in Enum.GetValues<CursorButtonTypeEnum>())
            {
                foreach (CursorButtonActionTypeEnum action in Enum.GetValues<CursorButtonActionTypeEnum>())
                {
                    FlowStep step = new FlowStep { FlowStepType = FlowStepTypeEnum.CURSOR_CLICK, CursorButtonType = button, CursorButtonActionType = action };
                    string written = new CursorClickWriter(step).Write();
                    FlowStep read = Read("Steps:\n" + written).Steps.Single();

                    (read.CursorButtonType, read.CursorButtonActionType).ShouldBe((button, action), written);
                }
            }
        }

        [Fact]
        public void A_plain_left_click_writes_no_button()
        {
            FlowStep step = new FlowStep { FlowStepType = FlowStepTypeEnum.CURSOR_CLICK, CursorButtonType = CursorButtonTypeEnum.LEFT_BUTTON, CursorButtonActionType = CursorButtonActionTypeEnum.SINGLE_CLICK };

            new CursorClickWriter(step).Write().ShouldBe("Click");
        }

        [Theory]
        [InlineData(TemplateMatchModeEnum.SHAPE)]
        [InlineData(TemplateMatchModeEnum.SHAPE_AND_BRIGHTNESS)]
        public void A_match_mode_is_read_back_as_written(TemplateMatchModeEnum mode)
        {
            FlowScriptSchema schema = Read($"Templates:\n  <[ a.png ]> click 1 2\nSteps:\nFind Image <[ x ]> template <[ a.png ]> match {ScriptKeywordCatalog.GetTextOfKeyword(mode)}");

            schema.Diagnostics.ShouldBeEmpty();
            schema.Steps.Single().TemplateMatchMode.ShouldBe(mode);
        }

        [Theory]
        [InlineData(ScalesWithEnum.DPI)]
        [InlineData(ScalesWithEnum.AREA)]
        public void What_an_area_scales_with_is_read_back_as_written(ScalesWithEnum scalesWith)
        {
            FlowScriptSchema schema = Read($"Areas:\n  <[ A ]> monitor primary   scales with {ScriptKeywordCatalog.GetTextOfKeyword(scalesWith)}");

            schema.Areas.Single().ScalesWith.ShouldBe(scalesWith);
        }

        // ================================================================
        // Numbers - each with its unit, or not a number the grammar takes
        // ================================================================

        [Theory]
        [InlineData("800ms", 800)]
        [InlineData("0ms", 0)]
        [InlineData("800", null)]
        [InlineData("15s", null)]
        [InlineData("1.5ms", null)]
        [InlineData("ms", null)]
        public void A_duration_is_a_whole_number_of_milliseconds(string text, int? milliseconds)
        {
            FlowScriptSchema schema = Read("Steps:\nWait " + text);

            if (milliseconds == null)
                schema.Diagnostics.ShouldHaveSingleItem().Code.ShouldBe(DiagnosticCodeEnum.TOKEN_UNEXPECTED);
            else
                schema.Steps.Single().WaitForMilliseconds.ShouldBe(milliseconds.Value);
        }

        [Theory]
        [InlineData("120dpi", 120)]
        [InlineData("120", null)]
        [InlineData("0dpi", null)]
        [InlineData("-96dpi", null)]
        [InlineData("dpi", null)]
        public void A_DPI_is_a_positive_number_ending_in_dpi(string text, int? dpi)
        {
            FlowScriptSchema schema = Read("Points:\n  <[ P ]> on screen offset 1 2 at " + text);

            if (dpi == null)
                schema.Diagnostics.ShouldHaveSingleItem().Code.ShouldBe(DiagnosticCodeEnum.TOKEN_UNEXPECTED);
            else
                schema.Points.Single().AuthoredDpi.ShouldBe(dpi.Value);
        }

        [Theory]
        [InlineData("800x600", 800, 600)]
        [InlineData("800x", 0, 0)]
        [InlineData("800", 0, 0)]
        [InlineData("1x2x3", 0, 0)]
        public void A_size_is_two_whole_numbers_joined_by_x(string text, int width, int height)
        {
            FlowScriptSchema schema = Read("Sizes: " + text);

            if (width == 0)
            {
                schema.Diagnostics.ShouldHaveSingleItem().Code.ShouldBe(DiagnosticCodeEnum.TOKEN_UNEXPECTED);
                return;
            }

            FlowViewport viewport = schema.Viewports.Single();
            (viewport.Width, viewport.Height).ShouldBe((width, height));
        }

        [Theory]
        [InlineData("120 40", 120, 40)]
        [InlineData("-4 10", -4, 10)]
        public void A_click_is_two_whole_numbers(string text, int x, int y)
        {
            FlowScriptSchema schema = Read("Templates:\n  <[ a.png ]> click " + text + "\nSteps:\nFind Image <[ Find ]> template <[ a.png ]>");
            FlowStepTemplate template = schema.Steps.Single().FlowStepTemplates.Single();

            (template.ClickOffsetX, template.ClickOffsetY).ShouldBe((x, y));
        }
    }
}
