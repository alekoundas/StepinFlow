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
            schema.Steps.Single(x => x.FlowStepType == FlowStepTypeEnum.GO_BACK).FlowStepReference.ShouldBeSameAs(total);
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

            script.ShouldContain("to nowhere");
            schema.Diagnostics.ShouldBeEmpty();
            FlowStep move = schema.Steps.Single();
            (move.FlowPoint, move.FlowStepReference).ShouldBe((null, null));
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
            Find Image  <[ Strict ]>   template <[ a.png ]> required  template <[ b.png ]> accuracy 0.9   match shape and brightness   in <[ Screen ]>
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
        public void Required_belongs_to_the_template_it_follows()
        {
            FlowScriptSchema schema = new Scanner().Read(HandWritten);

            schema.Steps[0].FlowStepTemplates.Select(x => x.IsRequired).ShouldBe([true, false]);
        }

        [Fact]
        public void The_header_facts_reach_the_steps_that_name_the_file()
        {
            FlowScriptSchema schema = new Scanner().Read(HandWritten);
            FlowStepTemplate described = schema.Steps[1].FlowStepTemplates.Single();

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
