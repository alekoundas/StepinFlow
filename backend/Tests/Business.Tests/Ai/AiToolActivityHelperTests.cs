using System.Reflection;
using Business.Ai.Helpers;
using Business.Ai.Tools;

namespace Business.Tests.Ai
{
    /// <summary>What the panel shows while the model looks things up: plain words, never a method name.</summary>
    public sealed class AiToolActivityHelperTests
    {
        private static readonly Dictionary<int, string> FLOW_NAMES = new Dictionary<int, string> { [17] = "Price - Crude Oil" };

        private static string Describe(string tool, params (string Name, object? Value)[] arguments)
        {
            return AiToolActivityHelper.Describe(tool, arguments.ToDictionary(x => x.Name, x => x.Value), FLOW_NAMES);
        }

        [Fact]
        public void A_flow_is_said_by_its_name()
        {
            Describe(nameof(DbQueryTools.GetFlowSteps), ("flowId", 17)).ShouldBe("Read the steps of Price - Crude Oil");
        }

        [Fact]
        public void A_flow_with_no_name_to_hand_is_said_by_its_id()
        {
            Describe(nameof(DbQueryTools.GetFlowSteps), ("flowId", 99)).ShouldBe("Read the steps of flow 99");
        }

        [Fact]
        public void Flow_zero_means_every_flow()
        {
            Describe(nameof(DbQueryTools.GetRuns), ("flowId", 0)).ShouldBe("Listed your latest executions");
            Describe(nameof(DbQueryTools.CountRunOutcomes), ("flowId", 0)).ShouldBe("Counted how your executions ended");
        }

        [Fact]
        public void An_execution_is_said_by_its_id()
        {
            Describe(nameof(DbQueryTools.GetRunSteps), ("executionId", 102)).ShouldBe("Read the steps of execution 102");
        }

        [Fact]
        public void A_search_says_what_it_looked_for()
        {
            Describe(nameof(DbQueryTools.SearchSteps), ("text", "chrome")).ShouldBe("Searched your steps for \"chrome\"");
            Describe(nameof(DbQueryTools.SearchFlows), ("text", "")).ShouldBe("Listed your flows");
        }

        [Fact]
        public void A_tool_it_does_not_know_still_reads_as_words()
        {
            Describe("SomethingNew").ShouldBe("Looked something up");
        }

        [Fact]
        public void Every_tool_the_model_can_call_has_its_own_words()
        {
            IEnumerable<string> tools = typeof(DbQueryTools)
                .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                .Where(x => x.GetCustomAttribute<System.ComponentModel.DescriptionAttribute>() != null)
                .Select(x => x.Name);

            foreach (string tool in tools)
                Describe(tool, ("flowId", 17), ("text", "x"), ("executionId", 1), ("flowStepId", 1)).ShouldNotBe("Looked something up", tool);
        }
    }
}
