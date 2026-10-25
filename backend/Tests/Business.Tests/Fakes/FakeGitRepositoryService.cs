using Business.Repository;
using Core.Models.Business.Git;
using Core.Models.Dtos;
using Core.Models.Dtos.Repository;

namespace Business.Tests.Fakes
{
    /// <summary>Remembers which flows a save said it changed, and writes nothing.</summary>
    public sealed class FakeGitRepositoryService : IGitRepositoryService
    {
        public List<int> Changes { get; } = [];

        /// <summary>What a flow's version reads as. Null for a repository that cannot be read.</summary>
        public GitFlowVersion? Version { get; set; } = new GitFlowVersion { CommitSha = new string('a', 40) };

        public void Changed(int flowId)
        {
            Changes.Add(flowId);
        }

        public Task StartBackgroundService(CancellationToken ct)
        {
            throw new NotImplementedException();
        }

        public Task StartAsync(CancellationToken ct)
        {
            throw new NotImplementedException();
        }

        public Task WriteAsync(IReadOnlyCollection<int> flowIds, CancellationToken ct)
        {
            throw new NotImplementedException();
        }

        public Task<RepositoryStatusDto> GetStatusAsync(CancellationToken ct)
        {
            throw new NotImplementedException();
        }

        public Task<List<RepositoryFileChangeDto>> GetChangesAsync(CancellationToken ct)
        {
            throw new NotImplementedException();
        }

        public Task<RepositoryFlowChangesDto?> GetFlowChangesAsync(int flowId, CancellationToken ct)
        {
            throw new NotImplementedException();
        }

        public Task<ResultDto<RepositoryCommitDto>> CommitAsync(RepositoryCommitRequestDto dto, CancellationToken ct)
        {
            throw new NotImplementedException();
        }

        public Task<List<RepositoryCommitDto>> GetFlowHistoryAsync(int flowId, CancellationToken ct)
        {
            throw new NotImplementedException();
        }

        public Task<GitFlowVersion> GetFlowVersionAsync(int flowId, CancellationToken ct)
        {
            if (Version == null)
                throw new InvalidOperationException("The repository could not be read.");

            return Task.FromResult(Version);
        }

        public Task<GitFlowFiles?> ReadFlowAtAsync(int flowId, string? commitSha, CancellationToken ct)
        {
            throw new NotImplementedException();
        }

        public Task<RepositoryFlowCommitDto?> GetFlowCommitAsync(RepositoryFlowCommitRequestDto dto, CancellationToken ct)
        {
            throw new NotImplementedException();
        }

        public Task<List<RepositoryBranchDto>> GetBranchesAsync(CancellationToken ct)
        {
            throw new NotImplementedException();
        }

        public Task<ResultDto<RepositoryBranchDto>> CreateBranchAsync(string name, CancellationToken ct)
        {
            throw new NotImplementedException();
        }

        public Task<ResultDto<GitBranchSwitch>> SwitchBranchAsync(string branch, Func<GitBranchSwitch, CancellationToken, Task<ResultDto<bool>>> applyAsync, CancellationToken ct)
        {
            throw new NotImplementedException();
        }

        public Task<ResultDto<RepositoryConnectDto>> ConnectAsync(RepositoryConnectRequestDto dto, CancellationToken ct)
        {
            throw new NotImplementedException();
        }

        public Task<ResultDto<RepositoryPullDto>> PreviewPullAsync(CancellationToken ct)
        {
            throw new NotImplementedException();
        }

        public Task<ResultDto<GitBranchSwitch>> PullAsync(string remoteSha, Func<GitBranchSwitch, CancellationToken, Task<ResultDto<bool>>> applyAsync, CancellationToken ct)
        {
            throw new NotImplementedException();
        }

        public Task<ResultDto<RepositoryPushDto>> PushAsync(CancellationToken ct)
        {
            throw new NotImplementedException();
        }
    }
}
