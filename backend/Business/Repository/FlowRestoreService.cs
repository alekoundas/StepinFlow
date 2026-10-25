using Business.FlowScript;
using Core.Enums;
using Core.Models.Business.Git;
using Core.Models.Dtos;
using Core.Models.Dtos.FlowScript;
using Core.Models.Dtos.Repository;
using Core.Ports;

namespace Business.Repository
{
    /// <summary>
    /// Discard and Restore. Apart from <see cref="GitRepositoryService"/> because the import saves
    /// through the data services, which tell that service what they changed; it reads the commit
    /// through that service all the same, so the folder still has the one owner.
    /// </summary>
    public sealed class FlowRestoreService : IFlowRestoreService
    {
        private readonly IGitRepositoryService _gitRepositoryService;
        private readonly IFlowScriptImporter _importer;
        private readonly IIpcBroadcastService _broadcast;

        public FlowRestoreService(IGitRepositoryService gitRepositoryService, IFlowScriptImporter importer, IIpcBroadcastService broadcast)
        {
            _gitRepositoryService = gitRepositoryService;
            _importer = importer;
            _broadcast = broadcast;
        }


        // ================================================================
        // Public methods
        // ================================================================

        public Task<ResultDto<FlowScriptImportResultDto>> DiscardAsync(int flowId, CancellationToken ct)
        {
            return BringBackAsync(flowId, null, "This flow has never been committed, so there is nothing to go back to.", ct);
        }

        public Task<ResultDto<FlowScriptImportResultDto>> RestoreAsync(int flowId, string commitSha, CancellationToken ct)
        {
            return BringBackAsync(flowId, commitSha, "That commit does not have this flow.", ct);
        }


        // ================================================================
        // Private methods
        // ================================================================

        private async Task<ResultDto<FlowScriptImportResultDto>> BringBackAsync(int flowId, string? commitSha, string notThere, CancellationToken ct)
        {
            GitFlowFiles? files = await _gitRepositoryService.ReadFlowAtAsync(flowId, commitSha, ct);
            if (files == null)
                return ResultDto<FlowScriptImportResultDto>.Failure(notThere);

            FlowScriptImportResultDto imported = await _importer.ImportTextAsync(files.Script, files.Templates, ct);
            if (!imported.IsSuccess)
            {
                string errors = string.Join(" ", imported.Errors.Select(x => x.Message));
                return ResultDto<FlowScriptImportResultDto>.Failure(errors);
            }

            // Written now rather than once the saves go quiet, so the Changes view shows the outcome.
            await _gitRepositoryService.WriteAsync([flowId], ct);

            RepositoryChangedDto changed = new RepositoryChangedDto { FlowIds = [flowId] };
            await _broadcast.SendAsync(BroadcastTypeEnum.REPOSITORY_CHANGED, changed);

            return ResultDto<FlowScriptImportResultDto>.Success(imported);
        }
    }
}
