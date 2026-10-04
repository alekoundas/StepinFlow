using Business.FlowScript.Catalogs;
using Business.FlowScript.Models.Text;
using Business.FlowScript.Syntax;
using Core.Enums;
using Core.Models.Database;

namespace Business.Tests.FlowScript
{
    /// <summary>
    /// Every word the grammar writes, read back. A word that meant one thing on the way out and
    /// another on the way in is a round trip that cannot be relied on.
    /// </summary>
    public sealed class SyntaxFactsTests
    {
        private static IReadOnlyList<ScriptToken> Tokens(string text)
        {
            return ScriptTokenizer.Read(text)[0].Tokens;
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

                string written = SyntaxFacts.For(step);
                ScriptKeyword? read = SyntaxFacts.ReadFirstKeyword(Tokens(written + " <[ name ]>"));

                if (written != keyword.Text || read != keyword)
                    wrong.Add($"{keyword.Text}: written \"{written}\", read back as \"{read?.Text}\"");
            }

            wrong.ShouldBeEmpty();
        }

        [Theory]
        [InlineData("Move Window process <[ x ]>", FlowStepTypeEnum.WINDOW_RELOCATE)]
        [InlineData("Wait Until No Image <[ x ]>", FlowStepTypeEnum.SEARCH_IMAGE)]
        [InlineData("Wait For Text <[ x ]>", FlowStepTypeEnum.SEARCH_TEXT)]
        [InlineData("Wait 800ms", FlowStepTypeEnum.WAIT)]
        public void The_longest_keyword_wins(string line, FlowStepTypeEnum type)
        {
            SyntaxFacts.ReadFirstKeyword(Tokens(line))!.Type.ShouldBe(type);
        }

        [Fact]
        public void Text_is_a_name_and_never_a_keyword()
        {
            SyntaxFacts.ReadFirstKeyword(Tokens("<[ Click ]> at match")).ShouldBeNull();
        }

        // Nothing inside is special, and the spaces at its edges are layout rather than text.
        [Theory]
        [InlineData("<[ He said \"hi\" ]>", "He said \"hi\"")]
        [InlineData(@"<[ C:\temp\ ]>", @"C:\temp\")]
        [InlineData("<[This text:\"Bruh\" ]>", "This text:\"Bruh\"")]
        [InlineData("<[                  This text:\"Bruh\" ]>", "This text:\"Bruh\"")]
        [InlineData("<[]>", "")]
        public void Text_is_everything_between_the_delimiters_trimmed(string line, string text)
        {
            ScriptToken token = Tokens(line).ShouldHaveSingleItem();

            token.IsQuoted.ShouldBeTrue();
            token.Text.ShouldBe(text);
        }

        [Fact]
        public void Text_that_never_closes_runs_to_the_end_of_the_line_and_says_so()
        {
            ScriptToken token = Tokens("<[ no end").ShouldHaveSingleItem();

            token.IsUnclosed.ShouldBeTrue();
            token.Text.ShouldBe("no end");
        }

        [Fact]
        public void Every_condition_is_written_and_read_back_the_same()
        {
            List<string> wrong = new List<string>();

            foreach (ConditionTypeEnum condition in Enum.GetValues<ConditionTypeEnum>())
            {
                FlowStep step = new FlowStep { ConditionType = condition, ConditionText = @"a ""quoted"" \value\", ConditionTextEnd = "9" };
                string written = SyntaxFacts.Condition(step);
                ConditionSyntax? read = SyntaxFacts.ReadCondition(Tokens(written), 0);

                string expectedText = string.Empty;
                if (condition != ConditionTypeEnum.IS_EMPTY && condition != ConditionTypeEnum.IS_NOT_EMPTY)
                    expectedText = step.ConditionText;

                string expectedEnd = string.Empty;
                if (condition == ConditionTypeEnum.BETWEEN)
                    expectedEnd = step.ConditionTextEnd;

                if (read == null || read.Type != condition || read.Text != expectedText || read.TextEnd != expectedEnd)
                    wrong.Add($"{condition}: written \"{written}\", read back as {read}");
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
            SyntaxFacts.ReadTitleMatch(Tokens(SyntaxFacts.TitleMatch(mode) + " <[ x ]>"), 0)!.As<TitleMatchModeEnum>().ShouldBe(mode);
        }

        [Theory]
        [InlineData(CursorScrollDirectionTypeEnum.UP)]
        [InlineData(CursorScrollDirectionTypeEnum.DOWN)]
        [InlineData(CursorScrollDirectionTypeEnum.LEFT)]
        [InlineData(CursorScrollDirectionTypeEnum.RIGHT)]
        public void A_scroll_direction_is_read_back_as_written(CursorScrollDirectionTypeEnum direction)
        {
            SyntaxFacts.ReadScrollDirection(SyntaxFacts.ScrollDirection(direction)).ShouldBe(direction);
        }

        [Fact]
        public void Every_button_and_action_is_read_back_as_written()
        {
            foreach (CursorButtonTypeEnum button in Enum.GetValues<CursorButtonTypeEnum>())
            {
                foreach (CursorButtonActionTypeEnum action in Enum.GetValues<CursorButtonActionTypeEnum>())
                {
                    string written = SyntaxFacts.Button(button, action);

                    CursorButtonTypeEnum readButton = CursorButtonTypeEnum.LEFT_BUTTON;
                    CursorButtonActionTypeEnum readAction = CursorButtonActionTypeEnum.SINGLE_CLICK;
                    foreach (string word in written.Split(' ', StringSplitOptions.RemoveEmptyEntries))
                    {
                        readButton = SyntaxFacts.ReadButtonSide(word) ?? readButton;
                        readAction = SyntaxFacts.ReadButtonAction(word) ?? readAction;
                    }

                    (readButton, readAction).ShouldBe((button, action), written);
                }
            }
        }

        [Fact]
        public void A_plain_left_click_writes_nothing()
        {
            SyntaxFacts.Button(CursorButtonTypeEnum.LEFT_BUTTON, CursorButtonActionTypeEnum.SINGLE_CLICK).ShouldBeEmpty();
        }

        [Theory]
        [InlineData(TemplateMatchModeEnum.SHAPE)]
        [InlineData(TemplateMatchModeEnum.SHAPE_AND_BRIGHTNESS)]
        public void A_match_mode_is_read_back_as_written(TemplateMatchModeEnum mode)
        {
            SyntaxFacts.ReadMatchMode(Tokens(SyntaxFacts.MatchMode(mode)), 0)!.As<TemplateMatchModeEnum>().ShouldBe(mode);
        }

        [Theory]
        [InlineData(ScalesWithEnum.DPI)]
        [InlineData(ScalesWithEnum.AREA)]
        public void What_an_area_scales_with_is_read_back_as_written(ScalesWithEnum scalesWith)
        {
            SyntaxFacts.ReadScalesWith(SyntaxFacts.ScalesWith(scalesWith)).ShouldBe(scalesWith);
        }

        // ================================================================
        // Numbers
        // ================================================================

        [Theory]
        [InlineData("15s", 15000)]
        [InlineData("1.5s", 1500)]
        [InlineData("800ms", 800)]
        [InlineData("250", 250)]
        public void A_duration_reads_in_whatever_unit_it_was_written(string text, int milliseconds)
        {
            SyntaxFacts.Milliseconds(text).ShouldBe(milliseconds);
        }

        [Theory]
        [InlineData("120dpi", 120)]
        [InlineData("120", null)]
        [InlineData("0dpi", null)]
        [InlineData("-96dpi", null)]
        [InlineData("dpi", null)]
        public void A_DPI_is_a_positive_number_ending_in_dpi(string text, int? dpi)
        {
            SyntaxFacts.ReadDpi(text).ShouldBe(dpi);
        }

        [Theory]
        [InlineData("120,40", ',', 120, 40)]
        [InlineData("-4,10", ',', -4, 10)]
        [InlineData("800x600", 'x', 800, 600)]
        public void A_pair_is_two_integers_joined_by_its_character(string text, char separator, int first, int second)
        {
            SyntaxFacts.ReadPair(text, separator).ShouldBe((first, second));
        }

        [Theory]
        [InlineData("1,2,3")]
        [InlineData("1.5,2")]
        [InlineData("120")]
        public void Anything_else_is_not_a_pair(string text)
        {
            SyntaxFacts.ReadPair(text, ',').ShouldBeNull();
        }
    }
}
