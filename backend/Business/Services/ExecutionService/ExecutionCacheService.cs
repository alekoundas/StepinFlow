using System.Drawing;

using Business.Services.AppSettingService;
using Business.Services.ScreenshotService;
using Core.Enums;
using Core.Helpers;
using Core.Models.Business;
using Core.Models.Database;

namespace Business.Services.ExecutionService
{
    /// <summary>
    /// What one run holds in memory while it walks.
    ///
    /// Two things, both bounded, so a run going for three weeks holds no more than one going for three seconds:
    /// 1) the execution steps a step below can still read - dropped as the walk leaves a branch.
    /// 2) the hits a FIND_ALL search came back with - dropped with the step that produced them.
    ///
    /// Screenshots are not held here. Each belongs to the step that took it and goes straight to
    /// that step, so there is nothing to roll out of a ring and nothing to hand over on failure.
    ///
    /// Nothing here is written anywhere. A run with history off keeps all of it and saves none of
    /// it, which is why turning history off can never change what a flow does.
    /// </summary>
    public sealed class ExecutionCacheService : IExecutionCacheService
    {
        private const int _screenshotQuality = 60;// Enough to see what was on screen

        private readonly IAppSettingService _appSettingService;
        private readonly IScreenshotService _screenshotService;

        private readonly Dictionary<int, ExecutionStep> _readableByStepId = new Dictionary<int, ExecutionStep>();                    //What a step below can still read, keyed by the flow step. One per step - a loop overwrites its own
        private readonly Dictionary<int, IReadOnlyList<Point>> _cachedPointsByStepId = new Dictionary<int, IReadOnlyList<Point>>();  //Every hit a FIND_ALL came back with

        private bool _keepsScreenshots;

        public ExecutionCacheService(
            IAppSettingService appSettingService,
            IScreenshotService screenshotService)
        {
            _appSettingService = appSettingService;
            _screenshotService = screenshotService;
        }

        public IReadOnlyDictionary<int, FlowStep> StepsById { get; private set; } = new Dictionary<int, FlowStep>();


        // ================================================================
        // Public methods
        // ================================================================

        public async Task ResetAsync(IReadOnlyDictionary<int, FlowStep> stepsById, bool keepsScreenshots, CancellationToken ct)
        {
            StepsById = stepsById;
            _keepsScreenshots = keepsScreenshots;

            _readableByStepId.Clear();
            _cachedPointsByStepId.Clear();
        }


        // Cache execution steps
        public void RecordExecutionStep(int flowStepId, ExecutionStep executionStep)
        {
            _readableByStepId[flowStepId] = executionStep;
        }

        public void ForgetExecutionStep(int flowStepId)
        {
            _readableByStepId.Remove(flowStepId);
            _cachedPointsByStepId.Remove(flowStepId);
        }

        public ExecutionStep? GetExecutionStepFrom(int flowStepId)
        {
            _readableByStepId.TryGetValue(flowStepId, out ExecutionStep? executionStep);
            return executionStep;
        }

        public Point? GetStepLocationFrom(int? flowStepReferenceId)
        {
            if (flowStepReferenceId == null)
                return null;

            ExecutionStep? executionStep = GetExecutionStepFrom(flowStepReferenceId.Value);
            return executionStep?.Location;
        }


        // Cache search matches
        public void RecordMatches(int flowStepId, IReadOnlyList<Point> matches)
        {
            _cachedPointsByStepId[flowStepId] = matches;
        }

        public IReadOnlyList<Point>? GetMatchesFrom(int flowStepId)
        {
            _cachedPointsByStepId.TryGetValue(flowStepId, out IReadOnlyList<Point>? matches);
            return matches;
        }


        public ExecutionScreenshot? RecordScreenshot(RawImage screenshot, FlowStep flowStep)
        {
            if (!_keepsScreenshots || screenshot.IsEmpty)
                return null;

            byte[] encoded = _screenshotService.Encode(screenshot, ScreenshotFormatEnum.JPEG, _screenshotQuality);

            return new ExecutionScreenshot(encoded, flowStep.Name, DateTime.Now);
        }

    }
}
