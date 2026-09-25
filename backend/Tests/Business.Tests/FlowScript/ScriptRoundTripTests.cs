using Business.FlowScript.Binding;
using Business.FlowScript.Diagnostics;
using Business.FlowScript.Syntax;
using Business.FlowScript.Text;
using Core.Enums;
using Core.Models.Database;


namespace Business.Tests.FlowScript
{
    public sealed class ScriptRoundTripTests
    {
        private static (FlowSyntax Document, List<Diagnostic> Errors) ReadAndBind(string script)
        {
            FlowSyntax document = new Parser().Read(script);
            List<Diagnostic> errors = new List<Diagnostic>();
            Binder.Resolve(document, errors);
            return (document, errors);
        }

        [Fact]
        public void Printing_what_was_read_gives_back_the_same_bytes()
        {
            string first = new Printer().Write(SampleFlow.Build());

            FlowSyntax document = new Parser().Read(first);
            document.Diagnostics.ShouldBeEmpty();

            List<Diagnostic> errors = new List<Diagnostic>();
            BoundFlow bound = Binder.Resolve(document, errors);
            errors.ShouldBeEmpty();

            new Printer().Write(bound).ShouldBe(first);
        }

        // The round trip proves the printer and the parser agree, not that either is right: a line
        // printed wrong and read back the same wrong way stays green. This file was read and approved.
        [Fact]
        public void The_sample_flow_prints_as_approved()
        {
            ApprovedFile.ShouldMatch(new Printer().Write(SampleFlow.Build()), "SampleFlow");
        }

        // ================================================================
        // What a person writing one by hand gets for what they leave out
        // ================================================================

        private const string HandWritten = """
            Flow:    Hand written
            Id:      8f14e45f-ea2b-4c3f-9f1a-77f0d2a3b112

            Areas:
              "Screen"   monitor primary   scales with dpi

            Templates:
              "described.png"   captured 800x600 at 144dpi

            Steps:
            Find Image  "Strict"   template "a.png" required  template "b.png" accuracy 0.9   match shape and brightness   in "Screen"
            Find Image  "Loose"    template "described.png"
            """;

        [Fact]
        public void A_hand_written_script_reads_without_complaint()
        {
            (FlowSyntax document, List<Diagnostic> errors) = ReadAndBind(HandWritten);

            document.Diagnostics.ShouldBeEmpty();
            errors.ShouldBeEmpty();
        }

        [Fact]
        public void The_mode_can_come_after_the_templates_it_governs()
        {
            (FlowSyntax document, _) = ReadAndBind(HandWritten);

            document.Steps[0].Step.TemplateMatchMode.ShouldBe(TemplateMatchModeEnum.SHAPE_AND_BRIGHTNESS);
            document.Steps[1].Step.TemplateMatchMode.ShouldBe(TemplateMatchModeEnum.SHAPE);
        }

        [Fact]
        public void A_template_with_no_accuracy_takes_its_modes_default()
        {
            (FlowSyntax document, _) = ReadAndBind(HandWritten);

            document.Steps[0].Templates[0].Accuracy.ShouldBe(0.95f);
            document.Steps[0].Templates[1].Accuracy.ShouldBe(0.9f);
            document.Steps[1].Templates[0].Accuracy.ShouldBe(0.8f);
        }

        [Fact]
        public void Required_belongs_to_the_template_it_follows()
        {
            (FlowSyntax document, _) = ReadAndBind(HandWritten);

            document.Steps[0].Templates.Select(x => x.IsRequired).ShouldBe([true, false]);
        }

        [Fact]
        public void The_header_facts_reach_the_steps_that_name_the_file()
        {
            (FlowSyntax document, _) = ReadAndBind(HandWritten);
            ScriptTemplate described = document.Steps[1].Templates[0];

            (described.AuthoredFlowAreaWidth, described.AuthoredFlowAreaHeight, described.AuthoredDpi).ShouldBe((800, 600, 144));
        }

        // The importer centres it on the picture, which needs the png - so the syntax leaves it open.
        [Fact]
        public void A_template_the_header_gives_no_click_leaves_it_to_the_importer()
        {
            (FlowSyntax document, _) = ReadAndBind(HandWritten);

            document.Steps[0].Templates[0].ClickOffset.ShouldBeNull();
            document.Steps[1].Templates[0].ClickOffset.ShouldBeNull();
        }

        [Fact]
        public void Monitor_primary_is_the_empty_device_name()
        {
            (FlowSyntax document, _) = ReadAndBind(HandWritten);
            FlowArea screen = document.Areas[0].Area;

            screen.Type.ShouldBe(FlowAreaTypeEnum.MONITOR);
            screen.MonitorDeviceName.ShouldBeEmpty();
            screen.ScalesWith.ShouldBe(ScalesWithEnum.DPI);
        }
    }
}
