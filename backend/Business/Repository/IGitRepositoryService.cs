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
        /// the files of flows that are gone or hidden.
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

        /// <summary>The local branches, by name.</summary>
        Task<List<RepositoryBranchDto>> GetBranchesAsync(CancellationToken ct);

        /// <summary>
        /// A new branch at the last commit, checked out. Nothing differs, so no flow changes, and
        /// changes not committed yet stay as they are.
        /// </summary>
        Task<ResultDto<RepositoryBranchDto>> CreateBranchAsync(string name, CancellationToken ct);

        /// <summary>
        /// Checks out another branch and hands what differs on it to <paramref name="applyAsync"/>,
        /// which brings the database along. The folder is held until it returns, so no flow is
        /// written into it halfway, and goes back to the branch it was on when it fails. Refused
        /// while anything is not committed.
        /// </summary>
        Task<ResultDto<GitBranchSwitch>> SwitchBranchAsync(string branch, Func<GitBranchSwitch, CancellationToken, Task<ResultDto<bool>>> applyAsync, CancellationToken ct);

        /// <summary>
        /// Sets the remote, HTTPS only, and saves a token when one is given. An empty remote is
        /// sent every branch; one with history comes back with what pulling the branch checked
        /// out would do, for the person to agree to. An address that does not answer is not kept.
        /// </summary>
        Task<ResultDto<RepositoryConnectDto>> ConnectAsync(RepositoryConnectRequestDto dto, CancellationToken ct);

        /// <summary>Fetches, and says what pulling the branch checked out would bring and drop.</summary>
        Task<ResultDto<RepositoryPullDto>> PreviewPullAsync(CancellationToken ct);

        /// <summary>
        /// Moves the branch checked out to the remote's commit, as SwitchBranchAsync moves it to
        /// another branch: commits never pushed are kept on a backup branch first, and go when the
        /// pull does not happen. Refused when the remote has moved since the preview.
        /// </summary>
        Task<ResultDto<GitBranchSwitch>> PullAsync(string remoteSha, Func<GitBranchSwitch, CancellationToken, Task<ResultDto<bool>>> applyAsync, CancellationToken ct);

        /// <summary>
        /// Sends the branch checked out to the remote's branch of the same name. Refused when the
        /// remote has commits the branch does not, which a pull brings.
        /// </summary>
        Task<ResultDto<RepositoryPushDto>> PushAsync(CancellationToken ct);
    }
}
