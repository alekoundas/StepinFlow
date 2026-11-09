using Business.FlowScript.Diagnostics;
using Business.FlowScript.Models;
using Business.FlowScript.Scanner;
using Business.FlowScript.Text;
using Core.Enums;
using Core.Models.Database;


namespace Business.Tests.FlowScript
{
    public sealed class ScriptRoundTripTests
    {
        // The scanner and the printer are mirrors over one model, so the round trip needs no database.
        [Fact]
        public void Printing_what_was_read_gives_back_the_same_bytes()
        {
            string first = new Printer().Write(SampleFlow.Build());

            FlowScriptSchema schema = new Scanner().Read(first);
            schema.Diagnostics.ShouldBeEmpty();

            new Printer().Write(schema).ShouldBe(first);
        }

        // The round trip proves the printer and the parser agree, not that either is right: a line
        // printed wrong and read back the same wrong way stays green. This file was read and approved.
        [Fact]
        public void The_sample_flow_prints_as_approved()
        {
            ApprovedFile.ShouldMatch(new Printer().Write(SampleFlow.Build()), "SampleFlow");
        }

        // Nothing inside <[ ]> is escaped, so quotes, backslashes and paths come back as they went out.
        [Theory]
        [InlineData("Login failed: \"bad password\"")]
        [InlineData(@"C:\temp\")]
        [InlineData(@"total: (\d+)")]
        [InlineData(@"\\server\share")]
        [InlineData("a\\\"b")]
        [InlineData(@"\")]
        public void Quoted_text_comes_back_as_it_was_written(string message)
        {
            FlowScriptSchema source = new FlowScriptSchema
            {
                Flow = new Flow { Name = "Quotes", PublicId = Guid.Parse("8f14e45f-ea2b-4c3f-9f1a-77f0d2a3b113") },
            };
            source.Steps.Add(new FlowStep { FlowStepType = FlowStepTypeEnum.NOTIFY, Message = message });

            FlowScriptSchema schema = new Scanner().Read(new Printer().Write(source));

            schema.Diagnostics.ShouldBeEmpty();
            schema.Steps.Single().Message.ShouldBe(message);
        }

        // An empty branch says nothing and is left out - unless its comment says why it is empty.
        [Fact]
        public void An_empty_branch_with_a_comment_is_kept()
        {
            FlowStep check = new FlowStep { FlowStepType = FlowStepTypeEnum.SEARCH_IMAGE, SearchMode = SearchModeEnum.FIND_BEST, Name = "Banner" };
            FlowStep success = new FlowStep { ParentFlowStep = check, OrderNumber = 0, FlowStepType = FlowStepTypeEnum.SUCCESS, CodeComment = "Nothing to do, it is already open." };
            FlowStep failure = new FlowStep { ParentFlowStep = check, OrderNumber = 1, FlowStepType = FlowStepTypeEnum.FAILURE };
            FlowScriptSchema source = new FlowScriptSchema
            {
                Flow = new Flow { Name = "Branches", PublicId = Guid.Parse("8f14e45f-ea2b-4c3f-9f1a-77f0d2a3b114") },
            };
            source.Steps.AddRange([check, success, failure]);

            FlowScriptSchema schema = new Scanner().Read(new Printer().Write(source));

            schema.Diagnostics.ShouldBeEmpty();
            schema.Steps.Select(x => x.FlowStepType).ShouldBe([FlowStepTypeEnum.SEARCH_IMAGE, FlowStepTypeEnum.SUCCESS]);
            schema.Steps[1].CodeComment.ShouldBe("Nothing to do, it is already open.");
        }

        // ================================================================
        // Named launches, commands and sub-flows
        // ================================================================

        private const string WithArea = "Flow:    Named\nId:      8f14e45f-ea2b-4c3f-9f1a-77f0d2a3b120\n\nAreas:\n  <[ App ]> window process <[ app.exe ]> started by <[ app.exe ]>\n\nSteps:\n";

        private const string Named = """
            Launch          <[ Start it ]>   opens <[ App ]>
            Shell           <[ Read it ]>   READ_CLIPBOARD <[  ]>
            Check Value     <[ Has it ]>   <[ {{Read it}} ]> is not empty
             Success:
              Notify          <[ got it ]>   with result of <[ Has it ]>
            Sub Flow        <[ Pay ]>   <[ Checkout ]>
            """;

        [Fact]
        public void A_launch_a_command_and_a_sub_flow_carry_their_names()
        {
            FlowScriptSchema schema = new Scanner().Read(LanguageLine(WithArea) + Named);

            schema.Diagnostics.ShouldBeEmpty();
            schema.Steps.Where(x => x.FlowStepType == FlowStepTypeEnum.SYSTEM_COMMAND || x.FlowStepType == FlowStepTypeEnum.SUB_FLOW)
                .Select(x => x.Name)
                .ShouldBe(["Start it", "Read it", "Pay"]);
            schema.SubFlows.Values.Single().Name.ShouldBe("Checkout");
        }

        // What a command said is read by its name, so the name has to be in the file.
        [Fact]
        public void A_check_reads_a_command_by_its_name_and_a_notify_reports_a_step_by_its()
        {
            FlowScriptSchema schema = new Scanner().Read(LanguageLine(WithArea) + Named);

            schema.Steps.Single(x => x.FlowStepType == FlowStepTypeEnum.CHECK_VALUE).FlowStepReference.ShouldBeSameAs(schema.Steps.Single(x => x.Name == "Read it"));
            schema.Steps.Single(x => x.FlowStepType == FlowStepTypeEnum.NOTIFY).FlowStepReference.ShouldBeSameAs(schema.Steps.Single(x => x.Name == "Has it"));
        }

        [Fact]
        public void The_names_and_the_reported_step_print_back_as_they_were_read()
        {
            string printed = new Printer().Write(new Scanner().Read(LanguageLine(WithArea) + Named));

            printed.ShouldContain("with result of <[ Has it ]>");
            new Printer().Write(new Scanner().Read(printed)).ShouldBe(printed);
        }

        // A word ahead of the line the step is written as anyway.
        [Fact]
        public void A_disabled_step_reads_and_prints_as_the_word_before_its_line()
        {
            FlowScriptSchema schema = new Scanner().Read(LanguageLine(WithArea) + "Disabled Wait            800ms");

            schema.Diagnostics.ShouldBeEmpty();
            schema.Steps.Single().ShouldSatisfyAllConditions(
                x => x.FlowStepType.ShouldBe(FlowStepTypeEnum.WAIT),
                x => x.WaitForMilliseconds.ShouldBe(800),
                x => x.IsDisabled.ShouldBeTrue());
            new Printer().Write(schema).ShouldContain("Disabled Wait            800ms");
        }

        [Fact]
        public void A_step_with_no_word_before_its_line_is_enabled()
        {
            FlowScriptSchema schema = new Scanner().Read(LanguageLine(WithArea) + "Wait            800ms");

            schema.Steps.Single().IsDisabled.ShouldBeFalse();
            new Printer().Write(schema).ShouldNotContain("Disabled");
        }

        // The five seconds a close waits unless told otherwise, and no force, are written as nothing.
        [Fact]
        public void A_close_with_neither_a_timeout_nor_force_reads_and_prints_as_just_its_window()
        {
            FlowScriptSchema schema = new Scanner().Read(LanguageLine(WithArea) + "Close Window    <[ Close it ]>   <[ App ]>");

            schema.Diagnostics.ShouldBeEmpty();
            schema.Steps.Single().ShouldSatisfyAllConditions(
                x => x.FlowStepType.ShouldBe(FlowStepTypeEnum.WINDOW_CLOSE),
                x => x.Name.ShouldBe("Close it"),
                x => x.FlowArea!.Name.ShouldBe("App"),
                x => x.TimeoutMilliseconds.ShouldBe(0),
                x => x.IsCloseForced.ShouldBeFalse());
            string printed = new Printer().Write(schema);
            printed.ShouldContain("Close Window    <[ Close it ]>   <[ App ]>");
            printed.ShouldNotContain("timeout");
            printed.ShouldNotContain("then force");
        }

        [Theory]
        [InlineData("Wait For Image", SearchModeEnum.WAIT_UNTIL_FOUND)]
        [InlineData("Wait For Still Image", SearchModeEnum.WAIT_UNTIL_STILL)]
        public void A_wait_for_an_image_says_by_its_keyword_whether_it_waits_until_still(string keyword, SearchModeEnum mode)
        {
            string line = keyword + "  <[ Given ]>   in <[ App ]>   timeout 1ms";
            FlowScriptSchema schema = new Scanner().Read(LanguageLine(WithArea) + line);

            schema.Diagnostics.ShouldBeEmpty();
            schema.Steps.Single().SearchMode.ShouldBe(mode);
            new Printer().Write(schema).ShouldContain(line + Environment.NewLine);
        }

        // The name comes first, because a check or typed text reads a command by it.
        [Theory]
        [InlineData("Launch          opens <[ App ]>")]
        [InlineData("Launch          <[ app.exe ]>")]
        [InlineData("Shell           KILL_PROCESS <[ app.exe ]>")]
        [InlineData("Shell           <[ cleanup.bat ]>")]
        [InlineData("Sub Flow        <[ Checkout ]>")]
        [InlineData("Focus Window    <[ App ]>")]
        [InlineData("Resize Window   <[ App ]>   size 1 2")]
        [InlineData("Move Window     <[ App ]>   to <[  ]>")]
        [InlineData("Close Window    <[ App ]>")]
        public void A_line_without_its_name_is_refused(string line)
        {
            new Scanner().Read(WithArea + line).Diagnostics.ShouldNotBeEmpty();
        }

        [Fact]
        public void A_quote_before_opens_is_the_steps_name()
        {
            FlowStep launch = new Scanner().Read(WithArea + "Launch          <[ Open it ]>   opens <[ App ]>").Steps.Single();

            (launch.Name, launch.FlowArea?.Name, launch.ShellArguments).ShouldBe(("Open it", "App", string.Empty));
        }

        private static string LanguageLine(string script)
        {
            return "Script Language: v1\n" + script;
        }

        // ================================================================
        // Lines the forms can save, which refused the whole import when they did not read back
        // ================================================================

        private static FlowScriptSchema Saved(params FlowStep[] steps)
        {
            FlowScriptSchema source = new FlowScriptSchema
            {
                Flow = new Flow { Name = "Saved", PublicId = Guid.Parse("8f14e45f-ea2b-4c3f-9f1a-77f0d2a3b121") },
            };
            source.Steps.AddRange(steps);

            return source;
        }

        // Its form is a text area, and a quote ends with its line.
        [Theory]
        [InlineData("first\nsecond", "Notify          <[ first ]>  <[ second ]>")]
        [InlineData("first\n\nthird", "Notify          <[ first ]>  <[  ]>  <[ third ]>")]
        [InlineData("\nsecond", "Notify          <[  ]>  <[ second ]>")]
        [InlineData("first\n", "Notify          <[ first ]>  <[  ]>")]
        [InlineData("\n", "Notify          <[  ]>  <[  ]>")]
        public void A_notify_of_several_lines_is_a_quote_for_each_and_comes_back_line_for_line(string message, string line)
        {
            string printed = new Printer().Write(Saved(new FlowStep { FlowStepType = FlowStepTypeEnum.NOTIFY, Message = message }));
            FlowScriptSchema schema = new Scanner().Read(printed);

            printed.ShouldContain(line);
            schema.Diagnostics.ShouldBeEmpty();
            schema.Steps.Single().Message.ShouldBe(message);
        }

        [Fact]
        public void A_notify_saved_with_windows_line_endings_comes_back_with_one_kind()
        {
            FlowScriptSchema schema = new Scanner().Read(new Printer().Write(Saved(new FlowStep { FlowStepType = FlowStepTypeEnum.NOTIFY, Message = "first\r\nsecond" })));

            schema.Diagnostics.ShouldBeEmpty();
            schema.Steps.Single().Message.ShouldBe("first\nsecond");
        }

        [Fact]
        public void A_notify_of_several_lines_still_takes_a_reported_step()
        {
            FlowScriptSchema schema = new Scanner().Read(LanguageLine(WithArea) + "Shell           <[ Read it ]>   <[ echo 1 ]>\n Success:\n  Notify          <[ first ]>  <[ second ]>   with result of <[ Read it ]>\n");

            schema.Diagnostics.ShouldBeEmpty();
            FlowStep notify = schema.Steps.Single(x => x.FlowStepType == FlowStepTypeEnum.NOTIFY);
            notify.Message.ShouldBe("first\nsecond");
            notify.FlowStepReference.ShouldBeSameAs(schema.Steps.Single(x => x.Name == "Read it"));
        }

        // A bot is this machine's, so the header lists it by name for the import to find, and the line
        // names it ahead of the result.
        [Fact]
        public void A_notify_names_a_bot_the_header_lists_and_prints_it_back_ahead_of_the_result()
        {
            string line = "Notify          <[ first ]>  <[ second ]>   through <[ Alerts ]>   with result of <[ Read it ]>";
            FlowScriptSchema schema = new Scanner().Read(WithBots("Alerts") + "Shell           <[ Read it ]>   <[ echo 1 ]>\n Success:\n  " + line + "\n");

            schema.Diagnostics.ShouldBeEmpty();
            FlowStep notify = schema.Steps.Single(x => x.FlowStepType == FlowStepTypeEnum.NOTIFY);
            notify.DiscordBot.ShouldBeNull();
            schema.DiscordBots[notify].ShouldSatisfyAllConditions(
                x => x.Name.ShouldBe("Alerts"),
                x => x.Line.ShouldBe(9));

            string printed = new Printer().Write(schema).ReplaceLineEndings("\n");
            printed.ShouldContain("\nDiscord Bots:\n  <[ Alerts ]>\n\nSteps:\n");
            printed.ShouldContain(line);
        }

        // Each Notify through a bot shares its one line, so the import finds it, and warns about it, once.
        [Fact]
        public void Notify_steps_through_one_bot_share_its_header_line()
        {
            FlowScriptSchema schema = new Scanner().Read(WithBots("Alerts") + "Notify          <[ first ]>   through <[ Alerts ]>\nNotify          <[ second ]>   through <[ alerts ]>\n");

            schema.Diagnostics.ShouldBeEmpty();
            List<FlowStep> notifies = schema.Steps.Where(x => x.FlowStepType == FlowStepTypeEnum.NOTIFY).ToList();
            schema.DiscordBots[notifies[1]].ShouldBeSameAs(schema.DiscordBots[notifies[0]]);

            List<string> printed = new Printer().Write(schema).ReplaceLineEndings("\n").Split('\n').ToList();
            printed.ShouldContain("Notify          <[ second ]>   through <[ Alerts ]>");
            printed.Count(x => x == "  <[ Alerts ]>").ShouldBe(1);
        }

        // Written from the steps, as the templates are, so a bot nothing sends through is not the flow's.
        [Fact]
        public void A_bot_no_notify_sends_through_is_not_written_back()
        {
            FlowScriptSchema schema = new Scanner().Read(WithBots("Alerts", "Night shift") + "Notify          <[ done ]>   through <[ Alerts ]>\n");

            schema.Diagnostics.ShouldBeEmpty();
            new Printer().Write(schema).ShouldNotContain("Night shift");
        }

        // A bot is named in Settings, not in the flow, so an area may have its name.
        [Fact]
        public void A_bot_may_share_its_name_with_an_area()
        {
            FlowScriptSchema schema = new Scanner().Read(WithBots("App") + "Notify          <[ done ]>   through <[ App ]>\n");

            schema.Diagnostics.ShouldBeEmpty();
        }

        // An empty quote is a bot saved with none, which the validator reports, as a step with no target is.
        [Fact]
        public void A_notify_through_an_empty_quote_has_no_bot()
        {
            FlowScriptSchema schema = new Scanner().Read(LanguageLine(WithArea) + "Notify          <[ done ]>   through <[  ]>\n");

            schema.Diagnostics.ShouldBeEmpty();
            schema.DiscordBots.ShouldBeEmpty();
        }

        private static string WithBots(params string[] names)
        {
            string bots = string.Concat(names.Select(x => $"  <[ {x} ]>\n"));

            return LanguageLine(WithArea).Replace("Steps:\n", $"Discord Bots:\n{bots}\nSteps:\n", StringComparison.Ordinal);
        }

        // A step moved out from under what it broke out of loses the link, and the validator reports it.
        [Fact]
        public void A_break_with_no_target_comes_back_with_none()
        {
            string printed = new Printer().Write(Saved(new FlowStep { FlowStepType = FlowStepTypeEnum.BREAK }));
            FlowScriptSchema schema = new Scanner().Read(printed);

            printed.ShouldContain("Break           out of <[  ]>");
            schema.Diagnostics.ShouldBeEmpty();
            schema.Steps.Single().FlowStepReference.ShouldBeNull();
        }

        [Fact]
        public void A_break_and_a_notify_reach_a_window_step_by_the_name_its_line_starts_with()
        {
            FlowStep focus = new FlowStep { FlowStepType = FlowStepTypeEnum.WINDOW_FOCUS, Name = "Focus window 2" };
            FlowStep success = new FlowStep { FlowStepType = FlowStepTypeEnum.SUCCESS, ParentFlowStep = focus };
            FlowStep notify = new FlowStep { FlowStepType = FlowStepTypeEnum.NOTIFY, Message = "m", ParentFlowStep = success, FlowStepReference = focus };
            FlowStep leave = new FlowStep { FlowStepType = FlowStepTypeEnum.BREAK, ParentFlowStep = success, OrderNumber = 1, FlowStepReference = focus };

            string printed = new Printer().Write(Saved(focus, success, notify, leave));
            FlowScriptSchema schema = new Scanner().Read(printed);

            printed.ShouldContain("Focus Window    <[ Focus window 2 ]>   <[  ]>");
            schema.Diagnostics.ShouldBeEmpty();
            FlowStep readFocus = schema.Steps.Single(x => x.FlowStepType == FlowStepTypeEnum.WINDOW_FOCUS);
            readFocus.Name.ShouldBe("Focus window 2");
            schema.Steps.Single(x => x.FlowStepType == FlowStepTypeEnum.NOTIFY).FlowStepReference.ShouldBeSameAs(readFocus);
            schema.Steps.Single(x => x.FlowStepType == FlowStepTypeEnum.BREAK).FlowStepReference.ShouldBeSameAs(readFocus);
            new Printer().Write(schema).ShouldBe(printed);
        }

        [Fact]
        public void A_disabled_window_step_starts_with_its_name_as_any_other()
        {
            FlowScriptSchema schema = new Scanner().Read(LanguageLine(WithArea) + "Disabled Focus Window    <[ Bring it forward ]>   <[ App ]>\n");

            schema.Diagnostics.ShouldBeEmpty();
            schema.Steps.Single().ShouldSatisfyAllConditions(
                x => x.IsDisabled.ShouldBeTrue(),
                x => x.Name.ShouldBe("Bring it forward"),
                x => x.FlowArea!.Name.ShouldBe("App"));
        }

        [Theory]
        [InlineData("Focus Window    <[ Given ]>   <[ App ]>")]
        [InlineData("Focus Window    <[ Given ]>   <[  ]>")]
        [InlineData("Resize Window   <[ Given ]>   <[ App ]>   size 1 2")]
        [InlineData("Move Window     <[ Given ]>   <[ App ]>   to <[  ]>")]
        [InlineData("Close Window    <[ Given ]>   <[ App ]>")]
        [InlineData("Close Window    <[ Given ]>   <[ App ]>   timeout 1ms   then force")]
        public void A_window_step_reads_its_name_first(string line)
        {
            FlowScriptSchema schema = new Scanner().Read(LanguageLine(WithArea) + line);

            schema.Diagnostics.ShouldBeEmpty();
            schema.Steps.Single().Name.ShouldBe("Given");
        }

        // Nothing can point at one, so it has no use for a name.
        [Theory]
        [InlineData("End Execution   <[ Given ]>   passed")]
        [InlineData("End Execution   passed   <[ Given ]>")]
        [InlineData("End Execution   failed  <[ why ]>   <[ Given ]>")]
        public void An_end_execution_line_takes_no_name(string line)
        {
            new Scanner().Read(LanguageLine(WithArea) + line).Diagnostics.ShouldNotBeEmpty();
        }

        [Fact]
        public void A_window_steps_name_is_held_to_the_one_set_of_names()
        {
            FlowScriptSchema schema = new Scanner().Read(LanguageLine(WithArea) + "Focus Window    <[ App ]>   <[ App ]>\n");

            schema.Diagnostics.ShouldHaveSingleItem().Code.ShouldBe(DiagnosticCodeEnum.NAME_DUPLICATE);
        }

        // Saved with nothing picked, so null, and the validator reports it after the import.
        [Theory]
        [InlineData("Move            to <[  ]>")]
        [InlineData("Move            to <[]>")]
        [InlineData("Drag            at <[  ]> to <[  ]>")]
        [InlineData("Break           out of <[  ]>")]
        [InlineData("Focus Window    <[ Given ]>   <[  ]>")]
        [InlineData("Move Window     <[ Given ]>   <[  ]>   to <[  ]>")]
        public void An_empty_quote_is_a_reference_to_nothing_rather_than_a_mistake(string line)
        {
            FlowScriptSchema schema = new Scanner().Read(LanguageLine(WithArea) + line);

            schema.Diagnostics.ShouldBeEmpty();
            schema.Steps.Single().ShouldSatisfyAllConditions(
                x => x.FlowStepReference.ShouldBeNull(),
                x => x.FlowStepReferenceEnd.ShouldBeNull(),
                x => x.FlowPoint.ShouldBeNull(),
                x => x.FlowPointEnd.ShouldBeNull());
        }

        [Theory]
        [InlineData("Break           out of <[ Nobody ]>")]
        [InlineData("Move            to <[ Nobody ]>")]
        [InlineData("Focus Window    <[ Given ]>   <[ Nothing like this ]>")]
        public void A_name_that_is_not_there_is_still_a_mistake(string line)
        {
            new Scanner().Read(LanguageLine(WithArea) + line).Diagnostics.ShouldHaveSingleItem().Code.ShouldBe(DiagnosticCodeEnum.NAME_UNKNOWN);
        }

        [Theory]
        [InlineData("Move            to nowhere")]
        [InlineData("Break           out of nowhere")]
        [InlineData("Focus Window    <[ Given ]>   <[ App ]>   named <[ Other ]>")]
        public void The_words_that_said_this_are_gone(string line)
        {
            new Scanner().Read(LanguageLine(WithArea) + line).Diagnostics.ShouldNotBeEmpty();
        }

        // ================================================================
        // Linked as it reads
        // ================================================================

        [Fact]
        public void Every_name_is_linked_to_the_row_declared_above()
        {
            FlowScriptSchema schema = new Scanner().Read(new Printer().Write(SampleFlow.Build()));

            FlowStep find = schema.Steps.Single(x => x.Name == "Find username field");
            FlowStep total = schema.Steps.Single(x => x.Name == "Read the total");

            find.FlowArea.ShouldBeSameAs(schema.Areas.Single(x => x.Name == "Browser"));
            schema.Areas.Single(x => x.Name == "Cart badge").ParentFlowArea.ShouldBeSameAs(schema.Areas.Single(x => x.Name == "Browser"));
            schema.Points.Single(x => x.Name == "Hamburger").FlowArea.ShouldBeSameAs(schema.Areas.Single(x => x.Name == "Browser"));
            schema.Steps.Single(x => x.FlowStepType == FlowStepTypeEnum.BREAK).FlowStepReference.ShouldBeSameAs(schema.Steps.Single(x => x.Name == "Retry"));
            schema.Steps.Single(x => x.FlowStepType == FlowStepTypeEnum.CHECK_VALUE).FlowStepReference.ShouldBeSameAs(total);
            schema.Steps.Single(x => x.FlowStepType == FlowStepTypeEnum.CURSOR_DRAG).FlowPointEnd.ShouldBeSameAs(schema.Points.Single(x => x.Name == "Origin"));
        }

        [Fact]
        public void A_step_is_linked_to_the_step_it_sits_under()
        {
            FlowScriptSchema schema = new Scanner().Read(new Printer().Write(SampleFlow.Build()));

            FlowStep find = schema.Steps.Single(x => x.Name == "Find username field");
            FlowStep success = schema.Steps.Single(x => x.ParentFlowStep == find && x.FlowStepType == FlowStepTypeEnum.SUCCESS);

            schema.Steps.Where(x => x.ParentFlowStep == success).Select(x => (x.FlowStepType, x.OrderNumber))
                .ShouldBe([(FlowStepTypeEnum.CURSOR_RELOCATE, 0), (FlowStepTypeEnum.CURSOR_CLICK, 1), (FlowStepTypeEnum.KEYBOARD_INPUT, 2)]);
        }

        // A move saved with no point is an error the validator reports, and an import keeps a flow
        // with errors - so the line still has to read back.
        [Fact]
        public void A_move_with_no_point_comes_back_with_no_point()
        {
            FlowScriptSchema source = new FlowScriptSchema
            {
                Flow = new Flow { Name = "Nowhere", PublicId = Guid.Parse("8f14e45f-ea2b-4c3f-9f1a-77f0d2a3b115") },
            };
            source.Steps.Add(new FlowStep { FlowStepType = FlowStepTypeEnum.CURSOR_RELOCATE });

            string script = new Printer().Write(source);
            FlowScriptSchema schema = new Scanner().Read(script);

            script.ShouldContain("to <[  ]>");
            schema.Diagnostics.ShouldBeEmpty();
            FlowStep move = schema.Steps.Single();
            (move.FlowPoint, move.FlowStepReference).ShouldBe((null, null));
        }

        // One # per line, and a blank line a bare #.
        [Theory]
        [InlineData("Logs in.")]
        [InlineData("Logs in.\n\nThen adds the backpack.")]
        [InlineData("Steps:\n1. open\n2. sign in")]
        [InlineData("# not a heading\n## nor a stage")]
        [InlineData("Opens <[ the menu ]> first")]
        public void The_description_comes_back_line_for_line(string description)
        {
            FlowScriptSchema source = new FlowScriptSchema
            {
                Flow = new Flow { Name = "Described", PublicId = Guid.Parse("8f14e45f-ea2b-4c3f-9f1a-77f0d2a3b116"), Description = description },
            };
            source.Steps.Add(new FlowStep { FlowStepType = FlowStepTypeEnum.WAIT, WaitForMilliseconds = 800 });

            FlowScriptSchema schema = new Scanner().Read(new Printer().Write(source));

            schema.Diagnostics.ShouldBeEmpty();
            schema.Flow.Description.ShouldBe(description);
        }

        // Each line is trimmed, as a comment is.
        [Fact]
        public void A_description_line_loses_its_indent()
        {
            FlowScriptSchema source = new FlowScriptSchema
            {
                Flow = new Flow { Name = "Indented", PublicId = Guid.Parse("8f14e45f-ea2b-4c3f-9f1a-77f0d2a3b118"), Description = "Steps:\n  1. open  " },
            };

            new Scanner().Read(new Printer().Write(source)).Flow.Description.ShouldBe("Steps:\n1. open");
        }

        [Fact]
        public void An_empty_description_is_left_out()
        {
            FlowScriptSchema source = new FlowScriptSchema
            {
                Flow = new Flow { Name = "Plain", PublicId = Guid.Parse("8f14e45f-ea2b-4c3f-9f1a-77f0d2a3b117") },
            };

            new Printer().Write(source).ShouldNotContain("Description:");
        }

        [Fact]
        public void The_main_area_comes_back_main()
        {
            FlowScriptSchema schema = new Scanner().Read(new Printer().Write(SampleFlow.Build()));

            schema.Areas.Where(x => x.IsMain).Select(x => x.Name).ShouldBe(["Browser"]);
        }

        // ================================================================
        // What a person writing one by hand gets for what they leave out
        // ================================================================

        private const string HandWritten = """
            Flow:    Hand written
            Id:      8f14e45f-ea2b-4c3f-9f1a-77f0d2a3b112

            Areas:
              <[ Screen ]>   monitor primary   scales with dpi

            Templates:
              <[ a.png ]>           click 1 2
              <[ b.png ]>           click 3 4
              <[ described.png ]>   click 5 6   captured 800x600 at 144dpi

            Steps:
            Find Image  <[ Strict ]>   template <[ a.png ]>  template <[ b.png ]> accuracy 0.9   match shape and brightness   in <[ Screen ]>
            Find Image  <[Loose]>    template <[described.png]>
            """;

        [Fact]
        public void A_hand_written_script_reads_without_complaint()
        {
            new Scanner().Read(HandWritten).Diagnostics.ShouldBeEmpty();
        }

        [Fact]
        public void The_mode_can_come_after_the_templates_it_governs()
        {
            FlowScriptSchema schema = new Scanner().Read(HandWritten);

            schema.Steps[0].TemplateMatchMode.ShouldBe(TemplateMatchModeEnum.SHAPE_AND_BRIGHTNESS);
            schema.Steps[1].TemplateMatchMode.ShouldBe(TemplateMatchModeEnum.SHAPE);
        }

        [Fact]
        public void A_template_with_no_accuracy_takes_its_modes_default()
        {
            FlowScriptSchema schema = new Scanner().Read(HandWritten);

            schema.Steps[0].FlowStepTemplates.Select(x => x.Accuracy).ShouldBe([0.95f, 0.9f]);
            schema.Steps[1].FlowStepTemplates.Single().Accuracy.ShouldBe(0.8f);
        }

        [Fact]
        public void The_header_facts_reach_the_steps_that_name_the_file()
        {
            FlowScriptSchema schema = new Scanner().Read(HandWritten);
            FlowTemplate described = schema.Steps[1].FlowStepTemplates.Single().FlowTemplate;

            (described.ClickOffsetX, described.ClickOffsetY).ShouldBe((5, 6));
            (described.AuthoredFlowAreaWidth, described.AuthoredFlowAreaHeight, described.AuthoredDpi).ShouldBe((800, 600, 144));
        }

        [Fact]
        public void Monitor_primary_is_the_empty_device_name()
        {
            FlowScriptSchema schema = new Scanner().Read(HandWritten);
            FlowArea screen = schema.Areas[0];

            screen.Type.ShouldBe(FlowAreaTypeEnum.MONITOR);
            screen.MonitorDeviceName.ShouldBeEmpty();
            screen.ScalesWith.ShouldBe(ScalesWithEnum.DPI);
        }
    }
}
