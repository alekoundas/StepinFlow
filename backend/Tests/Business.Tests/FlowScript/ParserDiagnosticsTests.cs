using Business.FlowScript.Catalogs;
using Business.FlowScript.Diagnostics;
using Business.FlowScript.Models.Text;
using Business.FlowScript.Scanner;
using Business.FlowScript.Text;
using Core.Enums;

namespace Business.Tests.FlowScript
{
    /// <summary>
    /// Every diagnostic, from the smallest script that earns it. The last test fails when a code is
    /// added without a line here, so a new rule cannot arrive untested.
    /// </summary>
    public sealed class ParserDiagnosticsTests
    {
        private const string Header = "Flow:    Test\nId:      8f14e45f-ea2b-4c3f-9f1a-77f0d2a3b111\n\n";

        public static TheoryData<DiagnosticCodeEnum, string> Cases { get; } = new TheoryData<DiagnosticCodeEnum, string>
        {
            { DiagnosticCodeEnum.TOKEN_UNEXPECTED, "Steps:\nNotify <[ never closes" },
            { DiagnosticCodeEnum.TOKEN_UNEXPECTED, "Steps:\nNotify" },
            { DiagnosticCodeEnum.TOKEN_UNEXPECTED, "Steps:\nLoop many times" },
            { DiagnosticCodeEnum.TOKEN_UNEXPECTED, "Steps:\nNotify <[ hi ]> there" },
            { DiagnosticCodeEnum.TOKEN_UNEXPECTED, "not a header" },
            { DiagnosticCodeEnum.TOKEN_UNEXPECTED, "Colour: red" },
            { DiagnosticCodeEnum.TOKEN_UNEXPECTED, "Id: nope" },
            { DiagnosticCodeEnum.TOKEN_UNEXPECTED, "Sizes: 1920by1080" },
            { DiagnosticCodeEnum.TOKEN_UNEXPECTED, "Areas:\n  Browser window process <[ chrome.exe ]>" },
            { DiagnosticCodeEnum.TOKEN_UNEXPECTED, "Areas:\n  <[ Browser ]> window chrome" },
            { DiagnosticCodeEnum.TOKEN_UNEXPECTED, "Areas:\n  <[ Browser ]> floating" },
            { DiagnosticCodeEnum.TOKEN_UNEXPECTED, "Areas:\n  <[ A ]> monitor primary\n  <[ B ]> inside <[ A ]> somewhere" },
            { DiagnosticCodeEnum.TOKEN_UNEXPECTED, "Areas:\n  <[ A ]> monitor primary sideways" },
            { DiagnosticCodeEnum.TOKEN_UNEXPECTED, "Areas:\n  <[ A ]> window process <[ x ]> title resembles <[ y ]>" },
            { DiagnosticCodeEnum.TOKEN_UNEXPECTED, "Inputs:\n  username" },
            { DiagnosticCodeEnum.TOKEN_UNEXPECTED, "Templates:\n  <[ a.png ]> click here" },
            { DiagnosticCodeEnum.TOKEN_UNEXPECTED, "Templates:\n  <[ a.png ]> captured 800x600" },
            { DiagnosticCodeEnum.TOKEN_UNEXPECTED, "Steps:\nFnid Image <[ x ]>" },
            { DiagnosticCodeEnum.TOKEN_UNEXPECTED, "Steps:\nCheck Text <[ x ]> nearly <[ y ]>" },
            { DiagnosticCodeEnum.TOKEN_UNEXPECTED, "Templates:\n  <[ a.png ]> click 1 2\nSteps:\nFind Image <[ x ]> template <[ a.png ]> quickly" },
            { DiagnosticCodeEnum.TOKEN_UNEXPECTED, "Steps:\nFind Image <[ x ]> accuracy 0.9" },
            { DiagnosticCodeEnum.TOKEN_UNEXPECTED, "Templates:\n  <[ a.png ]> click 1 2\nSteps:\nFind Image <[ x ]> template <[ a.png ]> match colour" },
            { DiagnosticCodeEnum.TOKEN_UNEXPECTED, "Steps:\nClick at <[ Origin ]>" },
            { DiagnosticCodeEnum.TOKEN_UNEXPECTED, "Steps:\nDrag at match to nowhere" },
            { DiagnosticCodeEnum.TOKEN_UNEXPECTED, "Steps:\nScroll sideways 3" },
            { DiagnosticCodeEnum.TOKEN_UNEXPECTED, "Areas:\n  <[ Browser ]> monitor primary\nSteps:\nScroll down 3 in <[ Browser ]>" },
            { DiagnosticCodeEnum.TOKEN_UNEXPECTED, "Steps:\nLoop each match in <[ Find ]>" },
            { DiagnosticCodeEnum.TOKEN_UNEXPECTED, "Steps:\nWait soon" },
            { DiagnosticCodeEnum.TOKEN_UNEXPECTED, "Steps:\nLoop often" },
            { DiagnosticCodeEnum.TOKEN_UNEXPECTED, "Steps:\nSystem EXPLODE" },
            { DiagnosticCodeEnum.TOKEN_UNEXPECTED, "Steps:\nFocus Window <[ chrome ]>" },
            { DiagnosticCodeEnum.TEMPLATE_UNKNOWN, "Steps:\nFind Image <[ x ]> template <[ a.png ]>" },
            { DiagnosticCodeEnum.TEMPLATE_DUPLICATE, "Templates:\n  <[ a.png ]> click 1 2\n  <[ a.png ]> click 3 4" },
            { DiagnosticCodeEnum.TEMPLATE_DUPLICATE, "Templates:\n  <[ a.png ]> click 1 2\n  <[ A.png ]> click 3 4" },
            { DiagnosticCodeEnum.LEADING_SPACES_UNEXPECTED, "Steps:\nWait 800ms\n  Wait 800ms" },
            { DiagnosticCodeEnum.COMMENT_UNATTACHED, "Steps:\nWait 800ms\n# nothing below" },
            { DiagnosticCodeEnum.NAME_UNKNOWN, "Steps:\nMove to <[ Nobody ]>" },
            { DiagnosticCodeEnum.NAME_UNKNOWN, "Points:\n  <[ P ]> inside <[ Nowhere ]> offset 1 2" },
            { DiagnosticCodeEnum.NAME_UNKNOWN, "Steps:\nGo Back to <[ Itself ]>" },
            { DiagnosticCodeEnum.NAME_DUPLICATE, "Steps:\n## Sign in\n## Sign in" },
            { DiagnosticCodeEnum.NAME_DUPLICATE, "Areas:\n  <[ Browser ]> monitor primary\nSteps:\n## browser" },
            { DiagnosticCodeEnum.AREA_TOO_DEEP, "Areas:\n  <[ A ]> monitor primary\n  <[ B ]> inside <[ A ]> ratio 0.10 0.10 size 0.50 0.50\n  <[ C ]> inside <[ B ]> ratio 0.10 0.10 size 0.50 0.50" },
        };

        private static List<Diagnostic> Read(string script)
        {
            return new Scanner().Read(script).Diagnostics;
        }

        [Theory]
        [MemberData(nameof(Cases))]
        public void Each_mistake_is_reported_with_its_code_and_line(DiagnosticCodeEnum code, string body)
        {
            List<Diagnostic> diagnostics = Read(Header + body);

            Diagnostic diagnostic = diagnostics.ShouldHaveSingleItem();
            diagnostic.Code.ShouldBe(code);
            diagnostic.Line.ShouldBe(Header.Split('\n').Length - 1 + body.Split('\n').Length);
        }

        // Every line the printer writes, with one word too many: whatever the line is, the word is
        // reported rather than dropped. Comments, stage headings and the flow's name are free text.
        [Fact]
        public void A_word_left_over_on_any_line_is_reported()
        {
            string[] lines = new Printer().Write(SampleFlow.Build()).Split('\n');
            List<string> silent = new List<string>();

            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i].TrimEnd();
                if (line.Length == 0 || line.TrimStart().StartsWith('#') || line.StartsWith("Flow:", StringComparison.Ordinal))
                    continue;

                string[] changed = (string[])lines.Clone();
                changed[i] = line + " extra";

                if (!new Scanner().Read(string.Join('\n', changed)).Diagnostics.Any(x => x.Line == i + 1))
                    silent.Add(line);
            }

            silent.ShouldBeEmpty();
        }

        // The message names the token, and everything that could have stood there - the optional
        // clauses passed over on the way included.
        [Theory]
        [InlineData("Steps:\nNotify <[ never closes", "Unexpected end of the line, expected \"]>\".")]
        [InlineData("Steps:\nNotify <[ hi ]> there", "Unexpected \"there\", expected the end of the line.")]
        [InlineData("Steps:\nFnid Image <[ x ]>", "Unexpected \"Fnid\".")]
        [InlineData("Steps:\nLoop often", "Unexpected \"often\", expected \"forever\" or a whole number.")]
        [InlineData("Steps:\nWait soon", "Unexpected \"soon\", expected a number ending in \"ms\".")]
        [InlineData("Points:\n  <[ Origin ]> offset 1 2", "Unexpected \"offset\", expected \"inside\" or \"on screen\".")]
        [InlineData("Points:\n  <[ Origin ]> on screen offset 1 2 offset 3 4", "Unexpected \"offset\", expected \"at\" or the end of the line.")]
        public void A_mistake_says_what_could_have_stood_there(string body, string message)
        {
            Read(Header + body).ShouldHaveSingleItem().Message.ShouldBe(message);
        }

        // A point is read in the order the grammar gives it, so a mistake is reported at the token
        // where it went wrong - past the end of the line when something is missing.
        [Theory]
        [InlineData("Origin on screen offset 1 2", 3)]
        [InlineData("<[ Origin ]> offset 1 2", 16)]
        [InlineData("<[ Origin ]> offset 1 2 inside <[ B ]>", 16)]
        [InlineData("<[ Origin ]> on screen ratio 0.5", 35)]
        [InlineData("<[ Origin ]> on screen offset 1 2 at nope", 40)]
        [InlineData("<[ Origin ]> on screen offset 1 2 offset 3 4", 37)]
        public void A_point_stops_at_the_first_token_out_of_place(string point, int column)
        {
            Diagnostic diagnostic = Read(Header + "Points:\n  " + point).ShouldHaveSingleItem();

            diagnostic.Code.ShouldBe(DiagnosticCodeEnum.TOKEN_UNEXPECTED);
            diagnostic.Column.ShouldBe(column);
        }

        // A step that fails still takes its place in the tree, so the lines under it are not each
        // reported as indented too far.
        [Fact]
        public void A_broken_step_is_one_mistake_not_one_per_line_under_it()
        {
            string body = "Steps:\nFind Image <[ x ]> quickly\n Success:\n  Wait 800ms";

            Read(Header + body).ShouldHaveSingleItem().Line.ShouldBe(5);
        }

        // A comment belongs to the step below it. Above anything else it belongs to nothing, and is
        // reported where it was written rather than handed to a step further down.
        [Theory]
        [InlineData("Areas:\n  # the browser\n  <[ A ]> monitor primary\nSteps:\nWait 800ms", 5, 3)]
        [InlineData("# before the steps\nSteps:\nWait 800ms", 4, 1)]
        [InlineData("Steps:\nWait 800ms\n# nothing below", 6, 1)]
        public void A_comment_with_no_step_below_it_is_reported_where_it_was_written(string body, int line, int column)
        {
            Diagnostic diagnostic = Read(Header + body).ShouldHaveSingleItem();

            diagnostic.Code.ShouldBe(DiagnosticCodeEnum.COMMENT_UNATTACHED);
            (diagnostic.Line, diagnostic.Column).ShouldBe((line, column));
        }

        // Every name is declared above its first use, so a name is checked on the line that uses it,
        // at the name, and one declared further down does not count.
        [Fact]
        public void A_name_declared_only_below_is_unknown_where_it_is_used()
        {
            Diagnostic diagnostic = Read(Header + "Steps:\nGo Back to <[ Retry ]>\n## Retry").ShouldHaveSingleItem();

            diagnostic.Code.ShouldBe(DiagnosticCodeEnum.NAME_UNKNOWN);
            (diagnostic.Line, diagnostic.Column).ShouldBe((5, 15));
        }

        // Reported at the quote the name was read from, not at the first quote on the line with the
        // same text - here the step's own name.
        [Fact]
        public void An_unknown_name_is_reported_at_its_own_quote()
        {
            Diagnostic diagnostic = Read(Header + "Steps:\nFind Image <[ Login ]> in <[ Login ]>").ShouldHaveSingleItem();

            diagnostic.Code.ShouldBe(DiagnosticCodeEnum.NAME_UNKNOWN);
            (diagnostic.Line, diagnostic.Column).ShouldBe((5, 30));
        }

        // The first one keeps the name, so the second is the one reported.
        [Fact]
        public void A_name_declared_twice_is_reported_at_the_second()
        {
            Diagnostic diagnostic = Read(Header + "Steps:\n## Sign in\nWait 800ms\n## Sign in").ShouldHaveSingleItem();

            diagnostic.Code.ShouldBe(DiagnosticCodeEnum.NAME_DUPLICATE);
            (diagnostic.Line, diagnostic.Column).ShouldBe((7, 4));
        }

        [Fact]
        public void A_file_with_no_flow_line_is_refused()
        {
            Read("Steps:\nWait 800ms").Single().Code.ShouldBe(DiagnosticCodeEnum.FLOW_LINE_MISSING);
        }

        // The name is the file the flow exports to, and the repository it lands in is cloned onto Windows too.
        [Theory]
        [InlineData("Login: smoke")]
        [InlineData("Is it up?")]
        [InlineData("CON")]
        public void A_flow_name_that_cannot_be_a_file_name_is_refused_on_its_line(string name)
        {
            Diagnostic diagnostic = Read($"Flow: {name}\nSteps:\nWait 800ms").ShouldHaveSingleItem();

            diagnostic.Code.ShouldBe(DiagnosticCodeEnum.FLOW_NAME_INVALID);
            diagnostic.Line.ShouldBe(1);
            diagnostic.Column.ShouldBe(7);
        }

        // A system action and a command preset are catalog words, so a number is never one.
        [Theory]
        [InlineData("System 1")]
        [InlineData("System 99")]
        [InlineData("Run 2 <[ notepad ]>")]
        public void A_system_action_or_preset_is_named_not_numbered(string line)
        {
            Read(Header + "Steps:\n" + line).Single().Code.ShouldBe(DiagnosticCodeEnum.TOKEN_UNEXPECTED);
        }

        // Each keyword alone is at most a syntax error: none reaches a step type no parser reads.
        [Fact]
        public void Every_step_keyword_has_a_parser()
        {
            foreach (ScriptKeyword keyword in ScriptKeywordCatalog.All.Where(x => x.Type is FlowStepTypeEnum))
                Should.NotThrow(() => new Scanner().Read(Header + "Steps:\n" + keyword.Text), keyword.Text);
        }

        [Fact]
        public void Every_code_the_script_can_produce_has_a_case_here()
        {
            HashSet<DiagnosticCodeEnum> covered = Cases.Select(x => (DiagnosticCodeEnum)x.Data.Item1).ToHashSet();
            covered.Add(DiagnosticCodeEnum.FLOW_LINE_MISSING);
            covered.Add(DiagnosticCodeEnum.FLOW_NAME_INVALID);

            // Raised by the importer, for a file that is not there or a name another flow has, not by anything in a script.
            covered.Add(DiagnosticCodeEnum.FILE_MISSING);
            covered.Add(DiagnosticCodeEnum.FLOW_NAME_TAKEN);

            Enum.GetValues<DiagnosticCodeEnum>().Except(covered).ShouldBeEmpty();
        }
    }
}
