using System.Linq.Expressions;

using Core.Models.Database;
using Core.Models.Dtos;
using DataAccess;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;

namespace Business.Executions
{
    /// <summary>
    /// Deletes executions: their steps, their row and their screenshots folder, then hands the
    /// space back to the disk.
    ///
    /// The steps go first, in one statement. Left to the cascades, deleting a flow executed for
    /// three weeks - four million steps - took half as long again.
    /// </summary>
    public sealed class ExecutionDeleteService : IExecutionDeleteService
    {
        private readonly IDbContextFactory<AppDbContext> _dbContextFactory;
        private readonly IExecutionEngine _engine;
        private readonly ILogger<ExecutionDeleteService> _logger;
        private readonly string _historyFolder;

        public ExecutionDeleteService(
            IDbContextFactory<AppDbContext> dbContextFactory,
            IExecutionEngine engine,
            ILogger<ExecutionDeleteService> logger,
            string historyFolder)
        {
            _dbContextFactory = dbContextFactory;
            _engine = engine;
            _logger = logger;
            _historyFolder = historyFolder;
        }


        // ================================================================
        // Public methods
        // ================================================================

        public async Task<ResultDto<bool>> DeleteAsync(int executionId, CancellationToken ct)
        {
            if (_engine.IsRunning && _engine.ExecutionId == executionId)
                return ResultDto<bool>.Failure("This execution is still going. Stop it, then delete it.");

            int deleted = await DeleteWhereAsync(x => x.Id == executionId, ct);
            if (deleted == 0)
                return ResultDto<bool>.Failure("That execution is not there any more.");

            return ResultDto<bool>.Success(true);
        }

        public async Task<ResultDto<int>> DeleteForFlowAsync(int flowId, CancellationToken ct)
        {
            int live = 0;
            if (_engine.IsRunning)
                live = _engine.ExecutionId;

            int deleted = await DeleteWhereAsync(x => x.FlowId == flowId && x.Id != live, ct);

            return ResultDto<int>.Success(deleted);
        }


        // ================================================================
        // Private methods
        // ================================================================

        private async Task<int> DeleteWhereAsync(Expression<Func<Execution, bool>> which, CancellationToken ct)
        {
            await using AppDbContext dbContext = await _dbContextFactory.CreateDbContextAsync(ct);

            List<DeletedExecution> executions = await dbContext.Executions
                .AsNoTracking()
                .Where(which)
                .Select(x => new DeletedExecution(x.Id, x.ScreenshotFolderName))
                .ToListAsync(ct);

            if (executions.Count == 0)
                return 0;

            List<int> ids = executions.Select(x => x.Id).ToList();

            await using (IDbContextTransaction transaction = await dbContext.Database.BeginTransactionAsync(ct))
            {
                await dbContext.ExecutionSteps.Where(x => ids.Contains(x.ExecutionId)).ExecuteDeleteAsync(ct);
                await dbContext.Executions.Where(x => ids.Contains(x.Id)).ExecuteDeleteAsync(ct);

                await transaction.CommitAsync(ct);
            }

            // Gone from the database whatever happens next, so nothing past here is cancelled.
            foreach (DeletedExecution execution in executions)
                DeleteFolder(execution.ScreenshotFolderName);

            await dbContext.ReclaimFreeSpaceAsync(CancellationToken.None);

            return executions.Count;
        }

        // Only a folder of the history's own: the name is the database's, and a name that would
        // reach outside is left alone.
        private void DeleteFolder(string? folderName)
        {
            if (string.IsNullOrEmpty(folderName) || Path.GetFileName(folderName) != folderName || folderName == "..")
                return;

            string folder = Path.Combine(_historyFolder, folderName);
            if (!Directory.Exists(folder))
                return;

            try
            {
                Directory.Delete(folder, recursive: true);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                _logger.LogWarning(ex, "Could not delete the screenshots folder {Folder}.", folder);// Open in Explorer, say. The rows are gone either way.
            }
        }


        // ================================================================
        // Private types
        // ================================================================

        private sealed record DeletedExecution(int Id, string? ScreenshotFolderName);
    }
}
