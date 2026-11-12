using System.Drawing;

using Core.Models.Business;
using Core.Models.Database;

namespace Business.Executions
{
    /// <summary>
    /// What a running flow holds in memory, and nothing else. No database, no writing, no history -
    /// that is IExecutionHistoryService. Everything in here is dropped as the walk leaves it behind.
    /// </summary>
    public interface IExecutionCacheService
    {
        IReadOnlyDictionary<int, FlowStep> StepsById { get; }

        /// <summary>A new execution: its steps, and the value of each input by name.</summary>
        Task ResetAsync(IReadOnlyDictionary<int, FlowStep> stepsById, IReadOnlyDictionary<string, string> inputValues, bool keepsScreenshots, CancellationToken ct);


        // Execution steps a step below can still read
        void RecordExecutionStep(int flowStepId, ExecutionStep executionStep);
        void ForgetExecutionStep(int flowStepId);
        ExecutionStep? GetExecutionStepFrom(int flowStepId);
        Point? GetStepLocationFrom(int? flowStepReferenceId);


        // Search matches
        void RecordMatches(int flowStepId, IReadOnlyList<Point> matches);
        IReadOnlyList<Point>? GetMatchesFrom(int flowStepId);


        // Variables
        /// <summary>
        /// Swaps {{name}} for what it stands for, using what this execution knows so far.
        ///
        /// Lives here because the values do: the flow's inputs and what an earlier step produced.
        /// A worker asks rather than assembling its own answer, so adding a source is one change
        /// here instead of one per worker.
        /// </summary>
        VariableTranslationResult TranslateVariables(string? text);


        // Screenshots
        /// <summary>The screenshot a step searched, handed back for that step to carry.</summary>
        ExecutionScreenshot? EncodeForHistory(RawImage screenshot, FlowStep flowStep);
    }
}
