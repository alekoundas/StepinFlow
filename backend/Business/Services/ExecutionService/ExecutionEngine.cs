using System.Diagnostics;

using Business.Services.ExecutionService.Workers;
using Core.Enums;
using Core.Interfaces;
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
    /// A singleton holding the one run that is allowed to be going: one mouse, one keyboard, one
    /// screen, so two flows racing for the cursor means one of them clicks where the other just
    /// moved. A second start is refused rather than queued.
    ///
    /// What each step does belongs to a worker and what runs next belongs to the navigator, so the
    /// only thing in here is the walking, the pause gate and cancellation.
    /// </summary>
    public sealed class ExecutionEngine : IExecutionEngine
    {
        private readonly IDbContextFactory<AppDbContext> _dbContextFactory;
        private readonly IStepWorkerFactory _workerFactory;
        private readonly IExecutionCacheService _cache;
        private readonly IExecutionHistoryService _history;
        private readonly IIpcBroadcastService _broadcastService;
        private readonly ILogger<ExecutionEngine> _logger;
        private ExecutionFlowWalker _walker = null!;


        private readonly object _lockObj = new object();
        private CancellationTokenSource _cancellation = new CancellationTokenSource();


        private int? _debuggerStepOverDepth;// Set while stepping over, so the walk runs until it is back at or above this depth
        private bool _debuggerSignalNextStep;// Step into and step over stay paused but let one step through
        private HashSet<int> _debuggerBreakpoints = new HashSet<int>();
        private int _currentDepth;
        private int? _currentStepId;

        public ExecutionEngine(
            IDbContextFactory<AppDbContext> dbContextFactory,
            IStepWorkerFactory workerFactory,
            IExecutionCacheService cache,
            IExecutionHistoryService history,
            IIpcBroadcastService broadcastService,
            ILogger<ExecutionEngine> logger)
        {
            _dbContextFactory = dbContextFactory;
            _workerFactory = workerFactory;
            _cache = cache;
            _history = history;
            _broadcastService = broadcastService;
            _logger = logger;
        }

        public int FlowId { get; private set; }
        public int ExecutionId { get; private set; }
        public bool IsRunning => State != RunStateEnum.FINISHED;
        public RunStateEnum State { get; private set; } = RunStateEnum.FINISHED;


        // ================================================================
        // Public methods
        // ================================================================

        public async Task<int> StartAsync(ExecutionStartDto dto, CancellationToken ct)
        {
            // Thread safe.
            lock (_lockObj)
            {
                if (IsRunning)
                    throw new InvalidOperationException("A flow is already running. Stop it first - two flows cannot share the mouse.");

                Reset(dto);
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
            _walker = new ExecutionFlowWalker(_cache);

            
            // Fire and forget:
            // Starts running synchronously on the calling thread and keeps running until it hits an await that genuinely suspends. Only then does it return an incomplete Task, and only then does StartAsync reach return ExecutionId.
            _ = Task.Run(async () =>
            {
                // Start execution
                try 
                {
                    using CancellationTokenSource linked = CancellationTokenSource.CreateLinkedTokenSource(ct, _cancellation.Token);
                    stepCount = await WalkAsync(linked.Token);
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

            // A cancelled source stays cancelled, so the last run's cannot be reused.
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
                    .Include(x => x.FlowStepImages)
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


        private async Task<int> WalkAsync(CancellationToken ct)
        {
            FlowStep? step = _walker.Start(FlowId);
            int stepCount = 0;

            while (step != null)
            {
                ct.ThrowIfCancellationRequested();

                _currentDepth = _walker.Depth;
                _currentStepId = step.Id;

                await DebugWaitAsync(step, ct);
                ct.ThrowIfCancellationRequested();

                ExecutionStep result = await ExecuteAsync(step, ct);
                stepCount++;

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

            return stepCount;
        }

        private async Task<ExecutionStep> ExecuteAsync(FlowStep step, CancellationToken ct)
        {
            IStepWorker worker = _workerFactory.GetWorker(step.FlowStepType);

            DateTime startedOn = DateTime.UtcNow;
            Stopwatch stopwatch = Stopwatch.StartNew();
            await BroadcastAsync(ExecutionEventDto.Started(ExecutionId, step));

            ExecutionStep executionStep = await worker.ExecuteAsync(step, _cache, ct);
            stopwatch.Stop();

            // A worker reports what happened; where it happened in the run is not its business.
            executionStep.StartedOn = startedOn;
            executionStep.DurationMilliseconds = (int)stopwatch.ElapsedMilliseconds;

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
    }
}
