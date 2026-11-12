using Business.Executions;
using Core.Helpers;
using Core.Models.Dtos;

using DataAccess;
using Microsoft.EntityFrameworkCore;

namespace Transport.Ipc.Handlers
{
    /// <summary>
    /// One screenshot, base64 for the page to draw: from memory while its execution is going and it
    /// is one of the last ones kept, otherwise off disk. The path is built here from the
    /// execution's folder and the step's file, so nothing the renderer sends decides what is read.
    /// </summary>
    public class GetExecutionStepScreenshotHandler
    {
        private readonly IDbContextFactory<AppDbContext> _dbContextFactory;
        private readonly IExecutionHistoryService _history;

        public GetExecutionStepScreenshotHandler(IDbContextFactory<AppDbContext> dbContextFactory, IExecutionHistoryService history)
        {
            _dbContextFactory = dbContextFactory;
            _history = history;
        }

        public async Task<ResultDto<string?>> HandleAsync(int executionStepId, CancellationToken ct)
        {
            byte[]? kept = _history.GetKeptScreenshot(executionStepId);
            if (kept != null)
                return ResultDto<string?>.Success(Convert.ToBase64String(kept));

            await using AppDbContext dbContext = await _dbContextFactory.CreateDbContextAsync(ct);

            ScreenshotLocation? location = await dbContext.ExecutionSteps
                .AsNoTracking()
                .Where(x => x.Id == executionStepId)
                .Select(x => new ScreenshotLocation(x.Execution.ScreenshotFolderName, x.ScreenshotFileName))
                .FirstOrDefaultAsync(ct);

            if (string.IsNullOrEmpty(location?.FolderName) || string.IsNullOrEmpty(location.FileName))
                return ResultDto<string?>.Success(null);

            string file = Path.Combine(PathHelper.GetExecutionHistoryDataPath(), location.FolderName, location.FileName);

            // A run whose folder has been cleared out is not an error - the step still says it kept
            // one, and the page says it is gone.
            if (!File.Exists(file))
                return ResultDto<string?>.Success(null);

            return ResultDto<string?>.Success(Convert.ToBase64String(await File.ReadAllBytesAsync(file, ct)));
        }


        // ================================================================
        // Private types
        // ================================================================

        private sealed record ScreenshotLocation(string? FolderName, string? FileName);
    }
}
