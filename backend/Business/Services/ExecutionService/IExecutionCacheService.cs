using System.Drawing;

using Core.Models.Business;
using Core.Models.Database;

namespace Business.Services.ExecutionService
{
    /// <summary>
    /// What a running flow holds in memory, and nothing else. No database, no writing, no history -
    /// that is IExecutionHistoryService. Everything in here is dropped as the walk leaves it behind.
    /// </summary>
    public interface IExecutionCacheService
    {
        IReadOnlyDictionary<int, FlowStep> StepsById { get; }

        Task ResetAsync(IReadOnlyDictionary<int, FlowStep> stepsById, bool keepsScreenshots, CancellationToken ct);


        // Execution steps a step below can still read
        void RecordExecutionStep(int flowStepId, ExecutionStep executionStep);
        void ForgetExecutionStep(int flowStepId);
        ExecutionStep? GetExecutionStepFrom(int flowStepId);
        Point? GetStepLocationFrom(int? flowStepReferenceId);


        // Search matches
        void RecordMatches(int flowStepId, IReadOnlyList<Point> matches);
        IReadOnlyList<Point>? GetMatchesFrom(int flowStepId);


        // Screenshots
        /// <summary>Rings it for the run-up, and hands it back for the step to carry.</summary>
        ExecutionScreenshot? RecordScreenshot(RawImage screenshot, FlowStep flowStep);

        IReadOnlyList<ExecutionScreenshot> TakeScreenshots();
    }
}
