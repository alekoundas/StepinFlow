using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

using Business.Services.ExecutionService.Workers;
using Core.Ports;
using Core.Enums;
using Core.Models.Database;
using Core.Models.Dtos;

using DataAccess;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Business.Services.ExecutionService
{
    /// <summary>
    /// Runs a flow.
    ///
    /// A singleton holding the one run that is allowed to be going
    /// A second start is refused.
    ///
    /// What each step does belongs to a worker and what runs next belongs to the navigator, so the
    /// only thing in here is the walking, the pause gate and cancellation.
    /// </summary>
    [SuppressMessage("Design", "CA1001:Types that own disposable fields should be disposable",
        Justification = "The source is never timed and never holds a registration - the one token linked to it is disposed by the using that made it - so there is nothing for Dispose to release. Owning a disposable field is not owning a resource. Making the engine IDisposable put Stop in a race with Reset over a source it could then cancel after disposal: 25 lines of guarding around a no-op.")]
    public sealed class ExecutionEngine : IExecutionEngine
    {
        private readonly IDbContextFactory<AppDbContext> _dbContextFactory;
        private readonly IStepWorkerFactory _workerFactory;
        private readonly IExecutionCacheService _cache;
        private readonly IExecutionHistoryService _history;
        private readonly IIpcBroadcastService _broadcastService;
        private readonly TimeProvider _timeProvider;
        private readonly ILogger<ExecutionEngine> _logger;
        private ExecutionFlowWalker _walker = null!;


        // Held for one thing: checking whether an execution is going and claiming it are two
        // steps, and two IPC messages arriving together would otherwise both get past the check
        // and start a walk. Two walks, one mouse.
        private readonly Lock _lockObj = new Lock();
        private CancellationTokenSource _cancellation = new CancellationTokenSource();


        private int? _debuggerStepOverDepth;// Set while stepping over, so the walk runs until it is back at or above this depth

        // Written by whichever thread the IPC call arrives on, read by the walk. Volatile rather
        // than locked: the pause gate reads them in a loop and must see the write that ends it.
        private volatile bool _debuggerSignalNextStep;// Step into and step over stay paused but let one step through
        private volatile RunStateEnum _state = RunStateEnum.FINISHED;
        private HashSet<int> _debuggerBreakpoints = new HashSet<int>();
        private int _currentDepth;
        private int? _currentStepId;

        public ExecutionEngine(
            IDbContextFactory<AppDbContext> dbContextFactory,
            IStepWorkerFactory workerFactory,
            IExecutionCacheService cache,
            IExecutionHistoryService history,
            IIpcBroadcastService broadcastService,
            TimeProvider timeProvider,
            ILogger<ExecutionEngine> logger)
        {
            _dbContextFactory = dbContextFactory;
            _workerFactory = workerFactory;
            _cache = cache;
            _history = history;
            _broadcastService = broadcastService;
            _timeProvider = timeProvider;
            _logger = logger;
        }

        public int FlowId { get; private set; }
        public int ExecutionId { get; private set; }
        public bool IsRunning
        {
            get { return State != RunStateEnum.FINISHED; }
        }
        public RunStateEnum State
        {
            get { return _state; }
            private set { _state = value; }
        }


        // ================================================================
        // Public methods
        // ================================================================

        [SuppressMessage("Reliability", "CA2016:Forward the CancellationToken parameter to methods that take one",
            Justification = "The walk outlives this call. ct is honoured inside through the linked source; handing it to Task.Run as well would let an already cancelled token stop the delegate before it starts, so nothing writes the history and the execution stays RUNNING.")]
        public async Task<int> StartAsync(ExecutionStartDto dto, CancellationToken ct)
        {
            // Thread safe.
            CancellationToken runToken;
            lock (_lockObj)
            {
                if (IsRunning)
                    throw new InvalidOperationException("A flow is already running. Stop it first - two flows cannot share the mouse.");

                Reset(dto);
                runToken = _cancellation.Token;
            }

            // Initialize.
            await using AppDbContext dbContext = await _dbContextFactory.CreateDbContextAsync(ct);
            ExecutionStatusEnum status = ExecutionStatusEnum.COMPLETED;
            string error = string.Empty;
            int? errorStepId = null;
            int stepCount = 0;

            // Load
            Dictionary<int, FlowStep> stepsById = await LoadReachableStepsAsync(dbContext, dto.FlowId, ct);
            await _cache.ResetAsync(stepsById, dto.HistoryLevel == ExecutionHistoryLevelEnum.STEPS_AND_IMAGES, ct);
            ExecutionId = await _history.StartAsync(dto, stepsById, ct); // History creates the Execution db row.
            _walker = new ExecutionFlowWalker(_cache, _timeProvider);


            // Fire and forget:
            // Starts running synchronously on the calling thread and keeps running until it hits an await that genuinely suspends. Only then does it return an incomplete Task, and only then does StartAsync reach return ExecutionId.
            _ = Task.Run(async () =>
            {
                // Start execution
                try
                {
                    using CancellationTokenSource linked = CancellationTokenSource.CreateLinkedTokenSource(ct, runToken);
                    WalkOutcome outcome = await WalkAsync(linked.Token);

                    stepCount = outcome.StepCount;
                    status = outcome.Status;
                    error = outcome.Reason;
                }
                catch (OperationCanceledException)  // Cancellation is how a run is stopped, not a fault.
                {
                    status = ExecutionStatusEnum.STOPPED;
                }
                catch (Exception ex) // Actual exception.
                {
                    status = ExecutionStatusEnum.ERRORED;
                    error = ex.Message;
                    errorStepId = _currentStepId;

                    _logger.LogWarning(ex, "Execution {ExecutionId} stopped at step {FlowStepId}.", ExecutionId, _currentStepId);
                }

                // Complete execution
                try
                {
                    await FinishAsync(status, error, errorStepId, stepCount);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Execution {ExecutionId} could not be closed off.", ExecutionId);
                    State = RunStateEnum.FINISHED;
                }
            });

            return ExecutionId;
        }

        public void Stop()
        {
            State = RunStateEnum.STOPPING;

            // A parked walk is sat in a Task.Delay on this token, so cancelling is what wakes it.
            _cancellation.Cancel();
        }

        public void Pause()
        {
            State = RunStateEnum.PAUSED;
        }

        public void Continue()
        {
            _debuggerStepOverDepth = null;
            State = RunStateEnum.RUNNING;
        }

        public void StepInto()
        {
            _debuggerStepOverDepth = null;
            State = RunStateEnum.PAUSED;
            _debuggerSignalNextStep = true;
        }

        public void StepOver()
        {
            _debuggerStepOverDepth = _currentDepth;
            State = RunStateEnum.PAUSED;
            _debuggerSignalNextStep = true;
        }

        public void SetBreakpoints(IEnumerable<int> flowStepIds)
        {
            _debuggerBreakpoints = flowStepIds.ToHashSet();
        }


        // ================================================================
        // Private methods
        // ================================================================

        private void Reset(ExecutionStartDto dto)
        {
            State = RunStateEnum.RUNNING;
            FlowId = dto.FlowId;
            ExecutionId = 0;

            _debuggerStepOverDepth = null;
            _currentDepth = 0;
            _currentStepId = null;

            _debuggerSignalNextStep = false;
            _debuggerBreakpoints = dto.Breakpoints.ToHashSet();

            // A cancelled source stays cancelled, so the last run's cannot be reused. Dropped
            // rather than disposed - see the suppression on the class.
            _cancellation = new CancellationTokenSource();
        }


        /// <summary>
        /// Every step of the flow and of every sub-flow it can reach, in one pass. RootId is what
        /// makes that a handful of queries instead of one per descent.
        /// </summary>
        private static async Task<Dictionary<int, FlowStep>> LoadReachableStepsAsync(AppDbContext dbContext, int flowId, CancellationToken ct)
        {
            Dictionary<int, FlowStep> stepsById = new Dictionary<int, FlowStep>();
            HashSet<int> loadedFlowIds = new HashSet<int>();
            Queue<int> pending = new Queue<int>();

            pending.Enqueue(flowId);

            while (pending.Count > 0)
            {
                int currentFlowId = pending.Dequeue();
                if (!loadedFlowIds.Add(currentFlowId))
                    continue;

                List<FlowStep> steps = await dbContext.FlowSteps
                    .AsNoTracking()
                    .Include(x => x.FlowStepTemplates)
                    .Where(x => x.RootId == currentFlowId)
                    .ToListAsync(ct);

                foreach (FlowStep step in steps)
                {
                    stepsById[step.Id] = step;

                    if (step.SubFlowId != null)
                        pending.Enqueue(step.SubFlowId.Value);
                }
            }

            return stepsById;
        }


        private async Task<WalkOutcome> WalkAsync(CancellationToken ct)
        {
            FlowStep? step = _walker.Start(FlowId);
            int stepCount = 0;
            WalkOutcome? verdict = null;

            while (step != null)
            {
                ct.ThrowIfCancellationRequested();

                _currentDepth = _walker.Depth;
                _currentStepId = step.Id;

                await DebugWaitAsync(step, ct);
                ct.ThrowIfCancellationRequested();

                ExecutionStep result = await ExecuteAsync(step, ct);
                stepCount++;

                // The one step that ends an execution on purpose, and says how it ended. It does
                // not stop the walk - the walker drops everything pending and pushes this step's
                // children, so the cleanup written under it still runs. The verdict is latched
                // because that cleanup must not be able to change what the flow already said.
                if (step.FlowStepType == FlowStepTypeEnum.END_EXECUTION && verdict == null)
                {
                    verdict = new WalkOutcome(
                        0,
                        step.EndExecutionAsSuccess ? ExecutionStatusEnum.COMPLETED : ExecutionStatusEnum.FAILED,
                        step.Message);
                }

                step = _walker.Next(step, result);

                // Working out what runs next can hand out a FIND_ALL search's remaining hits. Nobody
                // executed those, so they come back here to be recorded like everything else.
                foreach (ExecutionStep repeat in _walker.TakeMatchRepeats())
                {
                    await _history.RecordAsync(repeat);
                    await BroadcastAsync(ExecutionEventDto.Finished(ExecutionId, repeat));
                    stepCount++;
                }
            }

            // Nobody said how it went.
            if (verdict == null)
                return new WalkOutcome(stepCount, ExecutionStatusEnum.INCONCLUSIVE, string.Empty);

            return verdict with { StepCount = stepCount };
        }

        private async Task<ExecutionStep> ExecuteAsync(FlowStep step, CancellationToken ct)
        {
            IStepWorker worker = _workerFactory.GetWorker(step.FlowStepType);

            DateTime startedOn = _timeProvider.GetUtcNow().UtcDateTime;
            long startedAt = _timeProvider.GetTimestamp();
            await BroadcastAsync(ExecutionEventDto.Started(ExecutionId, step));

            ExecutionStep executionStep = await worker.ExecuteAsync(step, _cache, ct);

            // A worker reports what happened; where it happened in the run is not its business.
            executionStep.StartedOn = startedOn;
            executionStep.DurationMilliseconds = (int)_timeProvider.GetElapsedTime(startedAt).TotalMilliseconds;

            _walker.PlaceInRun(executionStep, step);

            _cache.RecordExecutionStep(step.Id, executionStep);
            await _history.RecordAsync(executionStep);
            await BroadcastAsync(ExecutionEventDto.Finished(ExecutionId, executionStep));

            return executionStep;
        }

        /// <summary>
        /// Parks before a step when the run is paused, on a breakpoint, or back at the depth a step
        /// over started from.
        /// </summary>
        private async Task DebugWaitAsync(FlowStep step, CancellationToken ct)
        {
            // A breakpoint wins over a step over. Landing on one inside the subtree being stepped
            // past abandons the step and parks here, which is what every debugger does.
            if (_debuggerBreakpoints.Contains(step.Id))
            {
                State = RunStateEnum.PAUSED;
                _debuggerStepOverDepth = null;
            }

            // Still inside the subtree being stepped past, so keep going without parking.
            if (_debuggerStepOverDepth != null && _currentDepth > _debuggerStepOverDepth.Value)
                return;

            _debuggerStepOverDepth = null;

            if (State != RunStateEnum.PAUSED)
                return;

            // Cleared before parking, so a press that landed while a slow step was still running
            // cannot skip this stop. One arriving from here on is kept and moves the walk along.
            _debuggerSignalNextStep = false;

            await BroadcastAsync(ExecutionEventDto.Paused(ExecutionId, step));

            // There is nothing to wait on but two fields, so they are read until one of them moves.
            // Only ever spins while somebody is sat looking at a paused run, and Stop cancels it out.
            while (State == RunStateEnum.PAUSED && !_debuggerSignalNextStep)
                await Task.Delay(50, ct);
        }

        private async Task FinishAsync(ExecutionStatusEnum status, string error, int? errorStepId, int stepCount)
        {
            State = RunStateEnum.FINISHED;

            await _history.CompleteAsync(status, error, errorStepId, stepCount);
            await BroadcastAsync(ExecutionEventDto.Ended(ExecutionId, status, error));
        }

        private async Task BroadcastAsync(ExecutionEventDto payload)
        {
            await _broadcastService.SendAsync(BroadcastTypeEnum.EXECUTION_EVENT, payload);
        }


        // ================================================================
        // Private types
        // ================================================================

        /// <summary>How the walk ended, so nothing has to be remembered in a field between steps.</summary>
        private sealed record WalkOutcome(int StepCount, ExecutionStatusEnum Status, string Reason);


    }
}
