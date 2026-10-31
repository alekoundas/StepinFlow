using Business.FlowScript.Diagnostics;
using Business.FlowScript.Models;
using Business.FlowScript.Scanner;
using Business.FlowScript.Text;
using Business.Recording.Generation;
using Core.Enums;
using Core.Models.Database;
using Core.Models.Dtos;

namespace Business.Tests.Recording
{
    /// <summary>
    /// The checks a model added, told from the script it was given by comparing, held to what the
    /// recording's screenshots showed, and taken out again with what they held kept.
    /// </summary>
    public sealed class ProposedChecksTests
    {
        private const string HEADER = """
            Flow:    Add to cart
            Id:      8f14e45f-ea2b-4c3f-9f1a-77f0d2a3b111
            Kind:    qa test

            Areas:
              <[ Browser ]>   main   window process <[ chrome.exe ]>

            Templates:
              <[ template-a7k2m.png ]>    click 48 16
              <[ template-c3x9w.png ]>    click 12 12

            Steps:

            """;

        private const string ADD_TO_CART = """
            Find Image  <[ Find Add to cart ]>   template <[ template-a7k2m.png ]>   in <[ Browser ]>   timeout 10000ms
             Failure:
              End Execution  failed  <[ no add button ]>
             Success:
              Move   to <[ Find Add to cart ]>
              Click

            """;

        // A check the model put after the click, with the test's pass under it.
        private const string REMOVE_CHECK = """
            Find Image  <[ Find Add to cart ]>   template <[ template-a7k2m.png ]>   in <[ Browser ]>   timeout 10000ms
             Failure:
              End Execution  failed  <[ no add button ]>
             Success:
              Move   to <[ Find Add to cart ]>
              Click
              Wait For Text  <[ Cart shows Remove ]>   contains <[ Remove ]>   in <[ Browser ]>   timeout 10000ms
               Failure:
                End Execution  failed  <[ the item never went into the cart ]>
               Success:
                End Execution  passed

            """;

        private static FlowScriptSchema Read(string steps)
        {
            FlowScriptSchema schema = new Scanner().Read(HEADER + steps);
            schema.IsValid.ShouldBeTrue(string.Join("\n", schema.Diagnostics.Select(x => $"{x.Line}: {x.Message}")));

            return schema;
        }

        private static List<string> Added(string before, string after, params string[] undecided)
        {
            return ProposedChecks.Find(Read(before), Read(after), undecided).Select(x => x.Name).ToList();
        }

        [Fact]
        public void A_check_nothing_moves_to_that_the_draft_did_not_have_is_added()
        {
            Added(ADD_TO_CART, REMOVE_CHECK).ShouldBe(["Cart shows Remove"]);
        }

        // Renamed, the tester's own check is still theirs, and a click's search is never a check.
        [Fact]
        public void A_click_s_search_and_the_tester_s_own_check_are_not_added()
        {
            string draft = ADD_TO_CART + """
                Wait For Image  <[ Check badge ]>   template <[ template-c3x9w.png ]>   in <[ Browser ]>   timeout 10000ms
                 Failure:
                  End Execution  failed  <[ no badge ]>

                """;
            string answer = draft.Replace("Find Add to cart", "Find the add button").Replace("Check badge", "The badge shows one item");

            Added(draft, answer).ShouldBeEmpty();
        }

        [Fact]
        public void A_wait_for_a_picture_a_click_searches_for_is_added()
        {
            string answer = ADD_TO_CART + """
                Wait For Image  <[ Add button is back ]>   template <[ template-a7k2m.png ]>   in <[ Browser ]>   timeout 10000ms
                 Failure:
                  End Execution  failed  <[ the add button did not come back ]>

                """;

            Added(ADD_TO_CART, answer).ShouldBe(["Add button is back"]);
        }

        // Before a revision it is in the flow, so only its name says it was never kept.
        [Fact]
        public void An_undecided_check_is_offered_again_after_a_revision_even_renamed()
        {
            string revised = REMOVE_CHECK.Replace("Cart shows Remove", "The item is in the cart");

            Added(REMOVE_CHECK, revised).ShouldBeEmpty();
            Added(REMOVE_CHECK, revised, "Cart shows Remove").ShouldBe(["The item is in the cart"]);
        }

        // Tested as an execution tests it, so case does not matter.
        [Fact]
        public void Words_no_screenshot_showed_are_refused()
        {
            List<FlowStep> added = ProposedChecks.Find(Read(ADD_TO_CART), Read(REMOVE_CHECK), []);

            Diagnostic refused = ProposedChecks.Unseen(added, ["Products", "Add to cart"]).ShouldHaveSingleItem();
            refused.Code.ShouldBe(DiagnosticCodeEnum.CHECK_TEXT_UNSEEN);
            refused.Message.ShouldContain("<[ Cart shows Remove ]> looks for <[ Remove ]>");

            ProposedChecks.Unseen(added, ["Products", "REMOVE"]).ShouldBeEmpty();
        }

        [Fact]
        public void Words_an_execution_fills_in_and_pictures_are_not_held_to_the_screen()
        {
            FlowScriptSchema answer = Read(REMOVE_CHECK.Replace("contains <[ Remove ]>", "contains <[ {{width}} ]>"));
            FlowStep words = ProposedChecks.Named(answer, "Cart shows Remove")!;

            ProposedChecks.IsReadOnScreen(words, ["Products"]).ShouldBeNull();
            ProposedChecks.IsReadOnScreen(ProposedChecks.Named(Read(REMOVE_CHECK), "Cart shows Remove")!, ["Remove"]).ShouldBe(true);
        }

        [Fact]
        public void The_Check_page_is_told_its_line_and_what_it_sits_under()
        {
            FlowScriptSchema answer = Read(REMOVE_CHECK);
            string script = new Printer().Write(answer);

            ProposedCheckDto proposed = ProposedChecks.ToDtos(ProposedChecks.Find(Read(ADD_TO_CART), answer, []), script, ["Remove"]).ShouldHaveSingleItem();

            proposed.Text.ShouldStartWith("Wait For Text");
            script.Split('\n')[proposed.Line - 1].ShouldContain("<[ Cart shows Remove ]>");
            proposed.Under.ShouldBe("Find Add to cart");
            proposed.IsReadOnScreen.ShouldBe(true);
        }

        // The pass the model nested under the check is the test's, not the check's.
        [Fact]
        public void A_dropped_check_leaves_what_its_Success_branch_held_in_its_place()
        {
            FlowScriptSchema flow = Read(REMOVE_CHECK);

            ProposedChecks.Drop(flow, "Cart shows Remove").ShouldBeNull();

            FlowScriptSchema dropped = new Scanner().Read(new Printer().Write(flow));
            dropped.IsValid.ShouldBeTrue();
            dropped.Steps.ShouldNotContain(x => x.Name == "Cart shows Remove");
            dropped.Steps.ShouldNotContain(x => x.Message == "the item never went into the cart");

            FlowStep passed = dropped.Steps.Single(x => x.FlowStepType == FlowStepTypeEnum.END_EXECUTION && x.EndExecutionAsSuccess);
            passed.ParentFlowStep!.ParentFlowStep!.Name.ShouldBe("Find Add to cart");

            List<FlowStepTypeEnum> success = dropped.Steps.Where(x => x.ParentFlowStep == passed.ParentFlowStep).OrderBy(x => x.OrderNumber).Select(x => x.FlowStepType).ToList();
            success.ShouldBe([FlowStepTypeEnum.CURSOR_RELOCATE, FlowStepTypeEnum.CURSOR_CLICK, FlowStepTypeEnum.END_EXECUTION]);
        }

        [Fact]
        public void A_check_another_step_uses_is_not_dropped()
        {
            FlowScriptSchema flow = Read(REMOVE_CHECK.Replace("    End Execution  passed", "    Notify  <[ Read {{Cart shows Remove}} ]>\n    End Execution  passed"));
            int count = flow.Steps.Count;

            ProposedChecks.Drop(flow, "Cart shows Remove").ShouldNotBeNull();
            flow.Steps.Count.ShouldBe(count);
        }

        [Fact]
        public void A_click_s_search_is_never_dropped()
        {
            ProposedChecks.Drop(Read(REMOVE_CHECK), "Find Add to cart").ShouldBe("The flow no longer has a check called \"Find Add to cart\".");
        }
    }
}
