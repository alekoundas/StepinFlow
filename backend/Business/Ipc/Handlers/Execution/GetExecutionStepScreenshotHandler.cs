using Core.Helpers;
using Core.Models.Dtos;
using Core.Models.Ipc;

using DataAccess;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Business.Ipc.Handlers
{
    /// <summary>
    /// One screenshot back off disk, base64 for the page to draw. The path is built here from the
    /// run's folder and the step's file, so nothing the renderer sends decides what is read.
    /// </summary>
    public class GetExecutionStepScreenshotHandler : IRequestHandler<GetExecutionStepScreenshotQuery, ResultDto<string?>>
    {
        private readonly IDbContextFactory<AppDbContext> _dbContextFactory;

        public GetExecutionStepScreenshotHandler(IDbContextFactory<AppDbContext> dbContextFactory)
        {
            _dbContextFactory = dbContextFactory;
        }

        public async Task<ResultDto<string?>> Handle(GetExecutionStepScreenshotQuery request, CancellationToken ct)
        {
            await using AppDbContext dbContext = await _dbContextFactory.CreateDbContextAsync(ct);

            ScreenshotLocation? location = await dbContext.ExecutionSteps
                .AsNoTracking()
                .Where(x => x.Id == request.executionStepId)
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
