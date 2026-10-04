using Business.FlowScript.Binding;
using Business.FlowScript.Diagnostics;
using Business.FlowScript.Models.Binding;
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
            { DiagnosticCodeEnum.QUOTE_UNCLOSED, "Steps:\nNotify <[ never closes" },
            { DiagnosticCodeEnum.QUOTE_INSIDE, "Steps:\nNotify <[ a <[ b ]>" },
            { DiagnosticCodeEnum.ARGUMENT_EXPECTED, "Steps:\nNotify" },
            { DiagnosticCodeEnum.NUMBER_MALFORMED, "Steps:\nLoop many times" },
            { DiagnosticCodeEnum.TOKEN_UNEXPECTED, "Steps:\nNotify <[ hi ]> there" },
            { DiagnosticCodeEnum.HEADER_MALFORMED, "not a header" },
            { DiagnosticCodeEnum.HEADER_UNKNOWN, "Colour: red" },
            { DiagnosticCodeEnum.PUBLIC_ID_MALFORMED, "Id: nope" },
            { DiagnosticCodeEnum.SIZE_MALFORMED, "Sizes: 1920by1080" },
            { DiagnosticCodeEnum.AREA_NAME_MISSING, "Areas:\n  Browser window process <[ chrome.exe ]>" },
            { DiagnosticCodeEnum.AREA_WINDOW_MALFORMED, "Areas:\n  <[ Browser ]> window chrome" },
            { DiagnosticCodeEnum.AREA_PLACEMENT_UNKNOWN, "Areas:\n  <[ Browser ]> floating" },
            { DiagnosticCodeEnum.PLACEMENT_MALFORMED, "Areas:\n  <[ A ]> monitor primary\n  <[ B ]> inside <[ A ]> somewhere" },
            { DiagnosticCodeEnum.AREA_ARGUMENT_UNKNOWN, "Areas:\n  <[ A ]> monitor primary sideways" },
            { DiagnosticCodeEnum.TITLE_MATCH_UNKNOWN, "Areas:\n  <[ A ]> window process <[ x ]> title resembles <[ y ]>" },
            { DiagnosticCodeEnum.CSV_COLUMN_MALFORMED, "Inputs:\n  username" },
            { DiagnosticCodeEnum.TEMPLATE_MALFORMED, "Templates:\n  <[ a.png ]> click here" },
            { DiagnosticCodeEnum.TEMPLATE_DUPLICATE, "Templates:\n  <[ a.png ]> click 1,2\n  <[ a.png ]> click 3,4" },
            { DiagnosticCodeEnum.INDENT_UNEXPECTED, "Steps:\nWait 800ms\n  Wait 800ms" },
            { DiagnosticCodeEnum.STEP_UNKNOWN, "Steps:\nFnid Image <[ x ]>" },
            { DiagnosticCodeEnum.CONDITION_MISSING, "Steps:\nCheck Text <[ x ]> nearly <[ y ]>" },
            { DiagnosticCodeEnum.SEARCH_ARGUMENT_UNKNOWN, "Steps:\nFind Image <[ x ]> template <[ a.png ]> quickly" },
            { DiagnosticCodeEnum.CLAUSE_WITHOUT_TEMPLATE, "Steps:\nFind Image <[ x ]> accuracy 0.9" },
            { DiagnosticCodeEnum.MATCH_MODE_UNKNOWN, "Steps:\nFind Image <[ x ]> template <[ a.png ]> match colour" },
            { DiagnosticCodeEnum.TARGET_MISSING, "Steps:\nClick point <[ Origin ]>" },
            { DiagnosticCodeEnum.DRAG_TARGET_MISSING, "Steps:\nDrag at match" },
            { DiagnosticCodeEnum.SCROLL_DIRECTION_UNKNOWN, "Steps:\nScroll sideways 3" },
            { DiagnosticCodeEnum.DURATION_MALFORMED, "Steps:\nWait soon" },
            { DiagnosticCodeEnum.LOOP_MALFORMED, "Steps:\nLoop often" },
            { DiagnosticCodeEnum.SYSTEM_ACTION_UNKNOWN, "Steps:\nSystem EXPLODE" },
            { DiagnosticCodeEnum.PROCESS_MISSING, "Steps:\nFocus Window <[ chrome ]>" },
            { DiagnosticCodeEnum.NAME_UNKNOWN, "Steps:\nClick at <[ Nobody ]>" },
        };

        private static List<Diagnostic> Read(string script)
        {
            FlowScriptSchema document = new Scanner().Read(script);
            List<Diagnostic> all = new List<Diagnostic>(document.Diagnostics);

            if (document.IsValid)
                Binder.Resolve(document, all);

            return all;
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

        [Fact]
        public void A_quote_closed_early_by_a_stray_close_says_why()
        {
            Diagnostic diagnostic = Read(Header + "Steps:\nNotify <[ a ]> b ]>").ShouldHaveSingleItem();

            diagnostic.Code.ShouldBe(DiagnosticCodeEnum.TOKEN_UNEXPECTED);
            diagnostic.Message.ShouldContain("can't contain");
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

        [Fact]
        public void A_file_with_no_flow_line_is_refused()
        {
            Read("Steps:\nWait 800ms").Single().Code.ShouldBe(DiagnosticCodeEnum.FLOW_LINE_MISSING);
        }

        // The printer writes names, so this only bites a hand-written or generated script - and
        // Enum.TryParse takes any number, defined or not.
        [Theory]
        [InlineData("System 1")]
        [InlineData("System 99")]
        public void A_system_action_is_named_not_numbered(string line)
        {
            Read(Header + "Steps:\n" + line).Single().Code.ShouldBe(DiagnosticCodeEnum.SYSTEM_ACTION_UNKNOWN);
        }

        [Fact]
        public void A_command_preset_is_named_not_numbered()
        {
            FlowScriptSchema document = new Scanner().Read(Header + "Steps:\nRun 2 <[ notepad ]>");

            document.Steps.Single().Step.RunCommandPreset.ShouldBe(RunCommandPresetEnum.CUSTOM);
        }

        [Fact]
        public void Every_code_the_script_can_produce_has_a_case_here()
        {
            HashSet<DiagnosticCodeEnum> covered = Cases.Select(x => (DiagnosticCodeEnum)x.Data.Item1).ToHashSet();
            covered.Add(DiagnosticCodeEnum.FLOW_LINE_MISSING);

            // Raised by the importer when the file is not there, not by anything in a script.
            covered.Add(DiagnosticCodeEnum.FILE_MISSING);

            Enum.GetValues<DiagnosticCodeEnum>().Except(covered).ShouldBeEmpty();
        }
    }
}
