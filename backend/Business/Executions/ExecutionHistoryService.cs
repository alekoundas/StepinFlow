using Core.Catalogs;
using Business.AppSettings;
using Business.FlowScript;
using Business.Repository;
using Core.Enums;
using Core.Helpers;
using Core.Models.Business;
using Core.Models.Business.Git;
using Core.Models.Database;
using Core.Models.Dtos;

using DataAccess;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;

namespace Business.Executions
{
    /// <summary>
    /// What an execution leaves behind: the Execution it hangs off, a step per thing that happened,
    /// and its last few screenshots.
    ///
    /// With history off nothing past the Execution row is kept, and the walk never notices - what a
    /// step reads comes from the cache, which is filled either way.
    ///
    /// Steps go down in batches, a failure with the rest: in a loop that looks for several buttons a
    /// pass, a search that finds nothing is most of the steps. A batch waits a second at most, so a
    /// slow flow still writes each step as it goes.
    ///
    /// Screenshots stay in memory, the last ones only, and are written once, as the execution ends.
    /// Written as they came, a search in a loop would write one on every pass.
    /// </summary>
    public sealed class ExecutionHistoryService : IExecutionHistoryService
    {
        private const int FLUSH_BATCH_SIZE = 200;// One transaction beats two hundred round trips
        private static readonly TimeSpan FLUSH_INTERVAL = TimeSpan.FromSeconds(1);

        private readonly IDbContextFactory<AppDbContext> _dbContextFactory;
        private readonly IAppSettingService _appSettingService;
        private readonly IGitRepositoryService _gitRepositoryService;
        private readonly IFlowScriptExporter _exporter;
        private readonly TimeProvider _timeProvider;
        private readonly ILogger<ExecutionHistoryService> _logger;
        private readonly string _historyFolder;

        private readonly List<ExecutionStep> _unwrittenExecutionSteps = new List<ExecutionStep>();//Every one that has run and not gone down yet. A loop of fifty passes is fifty of these

        // Oldest first. The walk adds to it while the screenshot handler reads it, hence the lock.
        private readonly Lock _keptLock = new Lock();
        private readonly List<KeptScreenshot> _kept = new List<KeptScreenshot>();

        private ExecutionHistoryLevelEnum _historyLevel;
        private string _flowName = string.Empty;
        private DateTime _startedOn;
        private int _screenshotLimit;
        private long _lastWriteAt;

        public ExecutionHistoryService(
            IDbContextFactory<AppDbContext> dbContextFactory,
            IAppSettingService appSettingService,
            IGitRepositoryService gitRepositoryService,
            IFlowScriptExporter exporter,
            TimeProvider timeProvider,
            ILogger<ExecutionHistoryService> logger,
            string historyFolder)
        {
            _dbContextFactory = dbContextFactory;
            _appSettingService = appSettingService;
            _gitRepositoryService = gitRepositoryService;
            _exporter = exporter;
            _timeProvider = timeProvider;
            _logger = logger;
            _historyFolder = historyFolder;
        }

        public int ExecutionId { get; private set; }


        // ================================================================
        // Public methods
        // ================================================================

        /// <summary>
        /// Opens an execution. The Execution row is written whatever the history level says, so an
        /// execution that kept nothing is still something the user can see happened.
        /// </summary>
        public async Task<int> StartAsync(ExecutionStartDto dto, IReadOnlyDictionary<int, FlowStep> stepsById, CancellationToken ct)
        {
            Reset(dto);

            _screenshotLimit = await _appSettingService.GetAsync(AppSettingCatalog.ExecutionScreenshotLimit, ct);
            _startedOn = _timeProvider.GetLocalNow().DateTime;

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

            await RecordVersionAsync(execution, ct);

            dbContext.Executions.Add(execution);
            await dbContext.SaveChangesAsync(ct);

            ExecutionId = execution.Id;
            return ExecutionId;
        }

        /// <summary>
        /// Takes a finished step, failed or not. It goes down with the next batch: two hundred
        /// waiting, or a second since the last one went.
        /// </summary>
        public async Task RecordAsync(ExecutionStep executionStep)
        {
            if (_historyLevel == ExecutionHistoryLevelEnum.NONE)
                return;

            executionStep.ExecutionId = ExecutionId;
            Keep(executionStep);
            _unwrittenExecutionSteps.Add(executionStep);

            if (_unwrittenExecutionSteps.Count >= FLUSH_BATCH_SIZE || _timeProvider.GetElapsedTime(_lastWriteAt) >= FLUSH_INTERVAL)
                await WriteExecutionStepsAsync();
        }

        public Task FlushAsync()
        {
            return WriteExecutionStepsAsync();
        }

        /// <summary>
        /// Closes the execution: the last steps go down, then the kept screenshots, each row that
        /// took one named after its file.
        /// </summary>
        public async Task CompleteAsync(ExecutionStatusEnum status, string error, int? errorFlowStepId, int stepCount)
        {
            await WriteExecutionStepsAsync();

            // Copied rather than taken, so the page can still be shown one while they are written.
            List<KeptScreenshot> kept;
            lock (_keptLock)
            {
                kept = _kept.ToList();
            }

            try
            {
                await using AppDbContext dbContext = await _dbContextFactory.CreateDbContextAsync();

                Execution? execution = await dbContext.Executions.FirstOrDefaultAsync(x => x.Id == ExecutionId);
                if (execution == null)
                    return;

                execution.Status = status;
                execution.CompletedAt = _timeProvider.GetUtcNow().UtcDateTime;
                execution.ErrorMessage = error;
                execution.ErrorFlowStepId = errorFlowStepId;
                execution.StepCount = stepCount;
                execution.ScreenshotFolderName = WriteKeptScreenshots(kept);

                await dbContext.SaveChangesAsync();
                await NameKeptScreenshotsAsync(dbContext, kept);
            }
            finally
            {
                ForgetKept();
            }
        }

        public byte[]? GetKeptScreenshot(int executionStepId)
        {
            if (executionStepId <= 0)
                return null;

            lock (_keptLock)
            {
                return _kept.FirstOrDefault(x => x.ExecutionSteps.Any(step => step.Id == executionStepId))?.Screenshot.Image;
            }
        }


        // ================================================================
        // Private methods
        // ================================================================

        private void Reset(ExecutionStartDto dto)
        {
            ExecutionId = 0;
            _historyLevel = dto.HistoryLevel;

            _unwrittenExecutionSteps.Clear();
            _lastWriteAt = _timeProvider.GetTimestamp();
            ForgetKept();

            _flowName = string.Empty;
        }

        // Git is the version history; what git cannot answer is kept here. The script goes in only
        // when the commit alone cannot say what executed. A repository that cannot be read costs the
        // record, never the execution.
        private async Task RecordVersionAsync(Execution execution, CancellationToken ct)
        {
            try
            {
                GitFlowVersion version = await _gitRepositoryService.GetFlowVersionAsync(execution.FlowId, ct);
                execution.CommitSha = version.CommitSha;
                execution.HasUncommittedChanges = version.HasUncommittedChanges;

                if (execution.CommitSha != null && execution.HasUncommittedChanges == false)
                    return;
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                _logger.LogWarning(exception, "Reading the repository for flow {FlowId} failed, so execution starts without its commit.", execution.FlowId);
            }

            try
            {
                execution.Script = await _exporter.RenderAsync(execution.FlowId, ct);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                _logger.LogWarning(exception, "Printing flow {FlowId} failed, so execution starts without its script.", execution.FlowId);
            }
        }

        private async Task WriteExecutionStepsAsync()
        {
            if (_unwrittenExecutionSteps.Count == 0)
                return;

            List<ExecutionStep> executionSteps = new List<ExecutionStep>(_unwrittenExecutionSteps);
            _unwrittenExecutionSteps.Clear();
            _lastWriteAt = _timeProvider.GetTimestamp();

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

        // The newest goes in and, past the limit, the oldest lets go of its picture: the cache can
        // hold its step for the rest of the execution.
        private void Keep(ExecutionStep executionStep)
        {
            ExecutionScreenshot? screenshot = executionStep.Screenshot;
            if (screenshot == null)
                return;

            if (_historyLevel != ExecutionHistoryLevelEnum.STEPS_AND_IMAGES || _screenshotLimit <= 0)
            {
                executionStep.Screenshot = null;
                return;
            }

            lock (_keptLock)
            {
                // A FIND_ALL's hits came off the one screenshot its search took, so they share its file.
                KeptScreenshot? same = _kept.FirstOrDefault(x => ReferenceEquals(x.Screenshot, screenshot));
                if (same != null)
                {
                    same.ExecutionSteps.Add(executionStep);
                    return;
                }

                _kept.Add(new KeptScreenshot(screenshot, new List<ExecutionStep> { executionStep }));
                if (_kept.Count <= _screenshotLimit)
                    return;

                foreach (ExecutionStep forgotten in _kept[0].ExecutionSteps)
                    forgotten.Screenshot = null;

                _kept.RemoveAt(0);
            }
        }

        // The folder is made here, so an execution that kept none leaves nothing behind. Null when
        // nothing was written.
        private string? WriteKeptScreenshots(List<KeptScreenshot> kept)
        {
            if (kept.Count == 0)
                return null;

            string folderName = PathHelper.GetExecutionFolderName(_flowName, _startedOn);
            string folder = Path.Combine(_historyFolder, folderName);
            int written = 0;

            foreach (KeptScreenshot screenshot in kept)
            {
                ExecutionStep first = screenshot.ExecutionSteps[0];
                string fileName = FileNameHelper.Clean($"{first.Sequence} {first.Name}", "screenshot") + ".jpg";

                try
                {
                    Directory.CreateDirectory(folder);
                    File.WriteAllBytes(Path.Combine(folder, fileName), screenshot.Screenshot.Image);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Could not write the screenshot {StepName} took.", screenshot.Screenshot.StepName);// A screenshot cant take down the execution.
                    continue;
                }

                foreach (ExecutionStep executionStep in screenshot.ExecutionSteps)
                    executionStep.ScreenshotFileName = fileName;

                written++;
            }

            if (written == 0)
                return null;

            return folderName;
        }

        // The rows went down without a file, since which screenshots would be the last was not known
        // yet. A row dropped from its batch has no Id and is passed over.
        private async Task NameKeptScreenshotsAsync(AppDbContext dbContext, List<KeptScreenshot> kept)
        {
            List<IGrouping<string, int>> files = kept
                .SelectMany(x => x.ExecutionSteps)
                .Where(x => x.Id > 0 && x.ScreenshotFileName != null)
                .GroupBy(x => x.ScreenshotFileName!, x => x.Id)
                .ToList();

            if (files.Count == 0)
                return;

            try
            {
                await using IDbContextTransaction transaction = await dbContext.Database.BeginTransactionAsync();

                foreach (IGrouping<string, int> file in files)
                {
                    List<int> ids = file.ToList();
                    string fileName = file.Key;

                    await dbContext.ExecutionSteps
                        .Where(x => ids.Contains(x.Id))
                        .ExecuteUpdateAsync(x => x.SetProperty(step => step.ScreenshotFileName, fileName));
                }

                await transaction.CommitAsync();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not name the {Count} screenshots execution {ExecutionId} kept.", files.Count, ExecutionId);
            }
        }

        private void ForgetKept()
        {
            lock (_keptLock)
            {
                foreach (ExecutionStep executionStep in _kept.SelectMany(x => x.ExecutionSteps))
                    executionStep.Screenshot = null;

                _kept.Clear();
            }
        }


        // ================================================================
        // Private types
        // ================================================================

        // One screenshot, and every row it belongs to: a search's, and its FIND_ALL hits'.
        private sealed record KeptScreenshot(ExecutionScreenshot Screenshot, List<ExecutionStep> ExecutionSteps);
    }
}
