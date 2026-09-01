using Business.Services.AppSettingService;
using Core.Enums;
using Core.Helpers;
using Core.Models.Business;
using Core.Models.Database;
using Core.Models.Dtos;

using DataAccess;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Business.Services.ExecutionService
{
    /// <summary>
    /// What a run leaves behind: the Execution it hangs off, a step per thing that happened, and
    /// the screenshots leading up to a failure.
    ///
    /// With history off nothing past the Execution row is kept, and the walk never notices - what a
    /// step reads comes from the cache, which is filled either way.
    ///
    /// Steps go down in batches. One insert per step is five hundred round trips in a flow of five
    /// hundred steps, which is how the old app's database reached eight gigabytes. A failure is the
    /// exception: it goes straight away, because that is the one somebody will go looking for.
    /// </summary>
    public sealed class ExecutionHistoryService : IExecutionHistoryService
    {
        private const int _flushBatchSize = 200;// One transaction beats two hundred round trips

        private readonly IDbContextFactory<AppDbContext> _dbContextFactory;
        private readonly IExecutionCacheService _cache;
        private readonly IAppSettingService _appSettingService;
        private readonly ILogger<ExecutionHistoryService> _logger;

        private readonly List<ExecutionStep> _unwrittenExecutionSteps = new List<ExecutionStep>();//Every one that has run and not gone down yet. A loop of fifty passes is fifty of these

        private ExecutionHistoryLevelEnum _historyLevel;
        private string _flowName = string.Empty;
        private string _runFolder = string.Empty;// Made on the first write, so a run that keeps nothing leaves nothing behind
        private int _screenshotLimit;
        private int _screenshotsWritten;

        public ExecutionHistoryService(
            IDbContextFactory<AppDbContext> dbContextFactory,
            IExecutionCacheService cache,
            IAppSettingService appSettingService,
            ILogger<ExecutionHistoryService> logger)
        {
            _dbContextFactory = dbContextFactory;
            _cache = cache;
            _appSettingService = appSettingService;
            _logger = logger;
        }

        public int ExecutionId { get; private set; }


        // ================================================================
        // Public methods
        // ================================================================

        /// <summary>
        /// Opens a run. The Execution row is written whatever the history level says, so a run that
        /// kept nothing is still something the user can see happened.
        /// </summary>
        public async Task<int> StartAsync(ExecutionStartDto dto, IReadOnlyDictionary<int, FlowStep> stepsById, CancellationToken ct)
        {
            Reset(dto);

            _screenshotLimit = await _appSettingService.GetAsync(AppSettingCatalog.ExecutionScreenshotLimit, ct);

            await using AppDbContext dbContext = await _dbContextFactory.CreateDbContextAsync(ct);

            _flowName = await dbContext.Flows
                .AsNoTracking()
                .Where(x => x.Id == dto.FlowId)
                .Select(x => x.Name)
                .FirstOrDefaultAsync(ct) ?? string.Empty;

            Execution execution = new Execution
            {
                FlowId = dto.FlowId,
                Status = ExecutionStatusEnum.RUNNING,
                HistoryLevel = dto.HistoryLevel,
                FlowStructureHash = FlowStructureHasher.Hash(stepsById.Values),
            };

            dbContext.Executions.Add(execution);
            await dbContext.SaveChangesAsync(ct);

            ExecutionId = execution.Id;
            return ExecutionId;
        }

        /// <summary>
        /// Takes a finished step. A failure takes the screenshots with it and goes down straight
        /// away, the rest wait for the batch.
        /// </summary>
        public async Task RecordAsync(ExecutionStep executionStep)
        {
            if (_historyLevel == ExecutionHistoryLevelEnum.NONE)
                return;

            executionStep.ExecutionId = ExecutionId;

            bool isFailure = executionStep.Outcome == StepOutcomeEnum.FAILURE;

            WriteScreenshots(executionStep, isFailure);

            _unwrittenExecutionSteps.Add(executionStep);

            if (isFailure || _unwrittenExecutionSteps.Count >= _flushBatchSize)
                await WriteExecutionStepsAsync();
        }

        public async Task CompleteAsync(ExecutionStatusEnum status, string error, int? errorFlowStepId, int stepCount)
        {
            await WriteExecutionStepsAsync();

            await using AppDbContext dbContext = await _dbContextFactory.CreateDbContextAsync();

            Execution? execution = await dbContext.Executions.FirstOrDefaultAsync(x => x.Id == ExecutionId);
            if (execution == null)
                return;

            execution.Status = status;
            execution.CompletedAt = DateTime.UtcNow;
            execution.ErrorMessage = error;
            execution.ErrorFlowStepId = errorFlowStepId;
            execution.StepCount = stepCount;

            // One folder for the whole run, so it belongs to the run. 
            if (_runFolder.Length > 0)
                execution.ScreenshotFolderName = Path.GetFileName(_runFolder);

            await dbContext.SaveChangesAsync();
        }


        // ================================================================
        // Private methods
        // ================================================================

        private void Reset(ExecutionStartDto dto)
        {
            ExecutionId = 0;
            _historyLevel = dto.HistoryLevel;

            _unwrittenExecutionSteps.Clear();

            _flowName = string.Empty;
            _runFolder = string.Empty;
            _screenshotsWritten = 0;
        }

        private async Task WriteExecutionStepsAsync()
        {
            if (_unwrittenExecutionSteps.Count == 0)
                return;

            List<ExecutionStep> executionSteps = new List<ExecutionStep>(_unwrittenExecutionSteps);
            _unwrittenExecutionSteps.Clear();

            try
            {
                await using AppDbContext dbContext = await _dbContextFactory.CreateDbContextAsync();

                dbContext.ExecutionSteps.AddRange(executionSteps);
                await dbContext.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Dropped {Count} execution steps.", executionSteps.Count);// History cant take down the execution.
            }
        }

        private void WriteScreenshots(ExecutionStep executionStep, bool isFailure)
        {
            if (_historyLevel != ExecutionHistoryLevelEnum.STEPS_AND_IMAGES)
                return;

            if (executionStep.Screenshot != null && _screenshotsWritten < _screenshotLimit)
            {
                executionStep.ScreenshotFileName = WriteScreenshot(executionStep.Screenshot, $"{executionStep.Sequence} {executionStep.Name}");
                if (executionStep.ScreenshotFileName != null)
                    _screenshotsWritten++;
            }

            if (!isFailure)
                return;

            IReadOnlyList<ExecutionScreenshot> runUp = _cache.TakeScreenshots();

            for (int i = 0; i < runUp.Count; i++)
                WriteScreenshot(runUp[i], $"{runUp[i].CapturedOn:HH.mm.ss.fff} {runUp[i].StepName} {i + 1} of {runUp.Count}");
        }

        // Actual write to disk.
        private string? WriteScreenshot(ExecutionScreenshot screenshot, string name)
        {
            try
            {
                if (_runFolder.Length == 0)
                    _runFolder = PathHelper.GetExecutionRunPath(_flowName, DateTime.Now);

                string fileName = $"{string.Concat(name.Split(Path.GetInvalidFileNameChars())).Trim()}.jpg";

                File.WriteAllBytes(Path.Combine(_runFolder, fileName), screenshot.Image);

                return fileName;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not write the screenshot {StepName} took.", screenshot.StepName);// Screenshot disk save, cant take down the execution.
                return null;
            }
        }
    }
}
