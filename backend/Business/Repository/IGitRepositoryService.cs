using Core.Models.Business.Git;
using Core.Models.Dtos;
using Core.Models.Dtos.Repository;

namespace Business.Repository
{
    /// <summary>
    /// The repository as the app keeps it and shows it: every flow written to the folder, what is not
    /// committed yet, committing it, and each flow's history.
    /// </summary>
    public interface IGitRepositoryService
    {
        /// <summary>
        /// A save changed this flow, so its files are written again once the saves go quiet. Never
        /// waits and never throws, so a save cannot fail over it.
        /// </summary>
        void Changed(int flowId);

        /// <summary>Starts the repository, then writes each flow a save changes, until the app stops.</summary>
        Task StartBackgroundService(CancellationToken ct);

        /// <summary>
        /// Makes the repository when there is none, with a first commit holding every flow. When there
        /// is one, brings every flow's files up to date.
        /// </summary>
        Task StartAsync(CancellationToken ct);

        /// <summary>
        /// Brings these flows' files up to date, and those of the flows that call them, then removes
        /// the files of flows that are gone.
        /// </summary>
        Task WriteAsync(IReadOnlyCollection<int> flowIds, CancellationToken ct);

        Task<RepositoryStatusDto> GetStatusAsync(CancellationToken ct);

        /// <summary>Every file not committed yet, grouped by the flow it belongs to.</summary>
        Task<List<RepositoryFileChangeDto>> GetChangesAsync(CancellationToken ct);

        /// <summary>One flow's files against the last commit, or null when there is no such flow.</summary>
        Task<RepositoryFlowChangesDto?> GetFlowChangesAsync(int flowId, CancellationToken ct);

        /// <summary>Commits these files, and only these.</summary>
        Task<ResultDto<RepositoryCommitDto>> CommitAsync(RepositoryCommitRequestDto dto, CancellationToken ct);

        /// <summary>Newest first, the commits that changed this flow's script or its images.</summary>
        Task<List<RepositoryCommitDto>> GetFlowHistoryAsync(int flowId, CancellationToken ct);

        /// <summary>
        /// The commit a flow is at as it starts executing, and whether it or a sub-flow it calls has
        /// changes not committed yet. Their files are written first, so a save a moment ago counts.
        /// </summary>
        Task<GitFlowVersion> GetFlowVersionAsync(int flowId, CancellationToken ct);

        /// <summary>
        /// A flow's script and images as a commit holds them - the last one when none is given - or
        /// null when that commit does not have the flow. Found by its Id, so a flow renamed since is
        /// found under its old name.
        /// </summary>
        Task<GitFlowFiles?> ReadFlowAtAsync(int flowId, string? commitSha, CancellationToken ct);

        /// <summary>What one commit changed in one flow, or null when either is not there.</summary>
        Task<RepositoryFlowCommitDto?> GetFlowCommitAsync(RepositoryFlowCommitRequestDto dto, CancellationToken ct);
    }
}
