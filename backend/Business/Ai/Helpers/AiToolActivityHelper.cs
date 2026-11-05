using Business.Ai.Tools;

namespace Business.Ai.Helpers
{
    /// <summary>A tool call as the user reads it: what it looked at, never a method name.</summary>
    public static class AiToolActivityHelper
    {
        public static string Describe(string toolName, IDictionary<string, object?>? arguments, IReadOnlyDictionary<int, string> flowNames)
        {
            string flow = FlowOf(arguments, flowNames);
            string text = ArgumentOf(arguments, "text");

            switch (toolName)
            {
                case nameof(AiDocumentTools.SearchAiDocuments):
                    return "Searched the user guide";

                case nameof(DbQueryTools.SearchFlows):
                    if (text.Length == 0)
                        return "Listed your flows";
                    return $"Searched your flows for \"{text}\"";

                case nameof(DbQueryTools.GetFlow):
                    return $"Read {flow}";

                case nameof(DbQueryTools.GetFlowSteps):
                    return $"Read the steps of {flow}";

                case nameof(DbQueryTools.GetFlowStepDetail):
                    return $"Read how step {ArgumentOf(arguments, "flowStepId")} is set up";

                case nameof(DbQueryTools.SearchSteps):
                    if (text.Length == 0)
                        return "Searched your steps";
                    return $"Searched your steps for \"{text}\"";

                case nameof(DbQueryTools.GetRuns):
                    if (flow.Length == 0)
                        return "Listed your latest executions";
                    return $"Listed the executions of {flow}";

                case nameof(DbQueryTools.GetRunSteps):
                    return $"Read the steps of execution {ArgumentOf(arguments, "executionId")}";

                case nameof(DbQueryTools.CountStepsByType):
                    return $"Counted the steps of {flow}";

                case nameof(DbQueryTools.GetFlowChecks):
                    return $"Read what {flow} checks";

                case nameof(DbQueryTools.CountRunOutcomes):
                    if (flow.Length == 0)
                        return "Counted how your executions ended";
                    return $"Counted how the executions of {flow} ended";

                case nameof(DbQueryTools.GetSettings):
                    return "Read your settings";

                case nameof(DbQueryTools.GetDiscordBots):
                    return "Read your Discord bots";

                default:
                    return "Looked something up";
            }
        }


        // ================================================================
        // Private methods
        // ================================================================

        // Empty for 0, which the tools take to mean every flow.
        private static string FlowOf(IDictionary<string, object?>? arguments, IReadOnlyDictionary<int, string> flowNames)
        {
            if (!int.TryParse(ArgumentOf(arguments, "flowId"), out int flowId) || flowId == 0)
                return string.Empty;

            if (flowNames.TryGetValue(flowId, out string? name))
                return name;

            return $"flow {flowId}";
        }

        private static string ArgumentOf(IDictionary<string, object?>? arguments, string name)
        {
            if (arguments == null || !arguments.TryGetValue(name, out object? value))
                return string.Empty;

            return value?.ToString() ?? string.Empty;
        }
    }
}
