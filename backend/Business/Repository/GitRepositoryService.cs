using System.Globalization;
using System.Text;
using System.Threading.Channels;

using Business.AppSettings;
using Business.FlowScript;
using Business.Repository.Initializer;
using Core.Catalogs;
using Core.Enums;
using Core.Helpers;
using Core.Models.Business.Git;
using Core.Models.Database;
using Core.Models.Dtos;
using Core.Models.Dtos.Repository;
using Core.Ports;
using DataAccess;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Business.Repository
{
    /// <summary>
    /// The only thing that works in the repository's folder, %LOCALAPPDATA%\StepinFlow_v2\Repository.
    /// It keeps the folder a copy of the database - every flow as its script and its template images,
    /// written when a save says it changed the flow and checked whole at startup - and reads and
    /// commits it through the git port.
    ///
    /// One thing at a time: a write, a read and a commit each wait their turn, so a commit never
    /// takes a file half written. Writing takes its turn a flow at a time, because requests arrive one
    /// at a time on one pipe, and a read waiting behind the whole startup would hold up every other.
    ///
    /// A flow is written by its export, which writes only the bytes that changed and removes the
    /// images the flow no longer uses. The files of a flow renamed or deleted are removed whole.
    ///
    /// A fetch and a push go outside the lock: the network can take seconds, and they touch only
    /// what the remote has, so the flows go on being written meanwhile.
    /// </summary>
    public sealed class GitRepositoryService : IGitRepositoryService, IDisposable
    {
        private const int HISTORY_LIMIT = 200;
        private const int SHORT_SHA_LENGTH = 7;
        private const string ID_FIELD = "Id:";
        private const string BACKUP_PREFIX = "backup/";
        private const string NO_REMOTE = "There is no remote yet. Connect one in Settings, Repository.";

        private static readonly TimeSpan QUIET = TimeSpan.FromMilliseconds(500);

        private readonly SemaphoreSlim _gate = new SemaphoreSlim(1, 1);
        private readonly Channel<int> _changes = Channel.CreateUnbounded<int>(new UnboundedChannelOptions { SingleReader = true }); // Keep all pending changes in a queue.

        private readonly IDbContextFactory<AppDbContext> _dbContextFactory;
        private readonly IFlowScriptExporter _exporter;
        private readonly IGitService _git;
        private readonly IAppSettingService _appSettingService;
        private readonly IIpcBroadcastService _broadcast;
        private readonly TimeProvider _timeProvider;
        private readonly ILogger<GitRepositoryService> _logger;
        private readonly string _folderPath;

        public GitRepositoryService(
            IDbContextFactory<AppDbContext> dbContextFactory,
            IFlowScriptExporter exporter,
            IGitService git,
            IAppSettingService appSettingService,
            IIpcBroadcastService broadcast,
            TimeProvider timeProvider,
            ILogger<GitRepositoryService> logger)
            : this(dbContextFactory, exporter, git, appSettingService, broadcast, timeProvider, logger, PathHelper.GetRepositoryPath())
        {
        }

        // A test points it at a folder of its own.
        internal GitRepositoryService(
            IDbContextFactory<AppDbContext> dbContextFactory,
            IFlowScriptExporter exporter,
            IGitService git,
            IAppSettingService appSettingService,
            IIpcBroadcastService broadcast,
            TimeProvider timeProvider,
            ILogger<GitRepositoryService> logger,
            string folderPath)
        {
            _dbContextFactory = dbContextFactory;
            _exporter = exporter;
            _git = git;
            _appSettingService = appSettingService;
            _broadcast = broadcast;
            _timeProvider = timeProvider;
            _logger = logger;
            _folderPath = folderPath;
        }

        public void Dispose()
        {
            _gate.Dispose();
        }


        // ================================================================
        // Public methods - writing
        // ================================================================

       

        public async Task StartBackgroundService(CancellationToken ct)
        {
            try
            {
                await StartAsync(ct);
                await BroadcastAsync([]);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                // A folder that cannot be written costs the copy, never the app.
                _logger.LogError(exception, "Preparing the repository at {Folder} failed.", _folderPath);
            }

            while (!ct.IsCancellationRequested)
            {
                HashSet<int> flowIds;
                try
                {
                    flowIds = await NextBatchAsync(ct);
                }
                catch (OperationCanceledException)
                {
                    return;
                }

                try
                {
                    await WriteAsync(flowIds, ct);
                    await BroadcastAsync(flowIds);
                }
                catch (Exception exception) when (exception is not OperationCanceledException)
                {
                    string names = string.Join(", ", flowIds);
                    _logger.LogError(exception, "Writing flows {FlowIds} to the repository failed.", names);
                }
            }
        }

        public void Changed(int flowId)
        {
            _changes.Writer.TryWrite(flowId);
        }

        public async Task StartAsync(CancellationToken ct)
        {
            bool created;

            await _gate.WaitAsync(ct);
            try
            {
                created = await GitRepositoryInitializer.InitializeAsync(_git, _folderPath, ct);
            }
            finally
            {
                _gate.Release();
            }

            List<Flow> flows = await FlowNamesAsync(ct);
            List<int> flowIds = flows.Select(x => x.Id).ToList();
            await WriteAsync(flowIds, ct);

            // At once, so the first edit shows as a change against it rather than inside it.
            if (!created)
                return;

            GitSignature author = await AuthorAsync(ct);
            string count = flows.Count == 1 ? "1 flow" : $"{flows.Count} flows";

            await _gate.WaitAsync(ct);
            try
            {
                List<string> paths = _git.Changes(_folderPath).Select(x => x.Path).ToList();
                _git.Commit(_folderPath, paths, $"Snapshot of {count}", author);
            }
            finally
            {
                _gate.Release();
            }
        }

        public async Task WriteAsync(IReadOnlyCollection<int> flowIds, CancellationToken ct)
        {
            List<int> subflowCallerFlowIds;

            // Get Flow Ids from flows calling this subflow.(In case the subflow name changed and the flow script doesnt make sense any more)
            await using AppDbContext dbContext = await _dbContextFactory.CreateDbContextAsync(ct);
            subflowCallerFlowIds = await dbContext.FlowSteps
                .AsNoTracking()
                .Where(x => x.SubFlowId != null && flowIds.Contains(x.SubFlowId.Value))
                .Select(x => x.RootId)
                .Distinct()
                .ToListAsync(ct);

            HashSet<int> wantedFlowIds = flowIds.Concat(subflowCallerFlowIds).ToHashSet();
            List<Flow> flows;

            // Read under the lock: a branch switch may have changed the flows while this waited.
            await _gate.WaitAsync(ct);
            try
            {
                flows = await FlowNamesAsync(ct);
                RemoveGoneFlows(flows);
            }
            finally
            {
                _gate.Release();
            }

            // Try to write the flows
            foreach (Flow flow in flows.Where(x => wantedFlowIds.Contains(x.Id)))
            {
                try
                {
                    await WriteFlowAsync(flow.Id, ct);
                }
                catch (Exception exception) when (exception is not OperationCanceledException)
                {
                    _logger.LogError(exception, "Writing flow {FlowName} to the repository failed.", flow.Name);
                }
            }
        }


        // ================================================================
        // Public methods - reading and committing
        // ================================================================

        public async Task<RepositoryStatusDto> GetStatusAsync(CancellationToken ct)
        {
            List<Flow> flows = await FlowNamesAsync(ct);

            await _gate.WaitAsync(ct);
            try
            {
                List<GitFileChange> changes = _git.Changes(_folderPath);
                GitCommit? last = _git.Log(_folderPath, [], 1).FirstOrDefault();
                GitSignature? configured = _git.ConfiguredAuthor(_folderPath);
                string? remoteUrl = _git.RemoteUrl(_folderPath);

                GitBranch? local = _git.Branches(_folderPath).FirstOrDefault(x => x.IsCurrent);
                GitBranch? remote = null;
                if (local != null)
                    remote = _git.RemoteBranches(_folderPath).FirstOrDefault(x => x.Name == local.Name);

                // As of the last fetch. A branch the remote does not have is all to push.
                int ahead = 0;
                int behind = 0;
                if (local != null && remoteUrl != null)
                {
                    ahead = _git.CommitsBetween(_folderPath, local.Sha, remote?.Sha).Count;
                    if (remote != null)
                        behind = _git.CommitsBetween(_folderPath, remote.Sha, local.Sha).Count;
                }

                List<RepositoryFlowStatusDto> changedFlows = new List<RepositoryFlowStatusDto>();
                foreach (Flow flow in flows)
                {
                    string flowFileName = FileNameHelper.Clean(flow.Name, "flow");
                    int count = changes.Count(x => RepositoryPathHelper.BelongsTo(x.Path, flowFileName));

                    if (count > 0)
                        changedFlows.Add(new RepositoryFlowStatusDto { FlowId = flow.Id, FlowName = flow.Name, ChangeCount = count });
                }

                return new RepositoryStatusDto
                {
                    FolderPath = _folderPath,
                    Branch = _git.CurrentBranch(_folderPath),
                    CommitCount = _git.CommitCount(_folderPath),
                    LastCommitAt = last?.When,
                    ChangeCount = changes.Count,
                    Flows = changedFlows,
                    ConfiguredAuthorName = configured?.Name ?? string.Empty,
                    ConfiguredAuthorEmail = configured?.Email ?? string.Empty,
                    RemoteUrl = remoteUrl,
                    IsBranchOnRemote = remote != null,
                    AheadCount = ahead,
                    BehindCount = behind,
                };
            }
            finally
            {
                _gate.Release();
            }
        }

        public async Task<List<RepositoryFileChangeDto>> GetChangesAsync(CancellationToken ct)
        {
            List<Flow> flows = await FlowNamesAsync(ct);

            await _gate.WaitAsync(ct);
            try
            {
                List<RepositoryFileChangeDto> files = new List<RepositoryFileChangeDto>();

                foreach (GitFileChange change in _git.Changes(_folderPath))
                {
                    string flowFileName = RepositoryPathHelper.FlowFileNameOf(change.Path);
                    Flow? flow = flows.FirstOrDefault(x => FileNameHelper.Clean(x.Name, "flow").Equals(flowFileName, StringComparison.OrdinalIgnoreCase));
                    bool isImage = RepositoryPathHelper.IsImage(change.Path);

                    byte[]? image = null;
                    if (isImage && change.Kind == GitChangeKindEnum.DELETED)
                        image = _git.ReadFile(_folderPath, null, change.Path);
                    else if (isImage)
                        image = await ReadDiskAsync(change.Path, ct);

                    files.Add(new RepositoryFileChangeDto
                    {
                        Path = change.Path,
                        Kind = change.Kind,
                        FlowId = flow?.Id,
                        FlowName = flow?.Name ?? flowFileName,
                        IsImage = isImage,
                        Image = image,
                    });
                }

                // A flow's script before its images, as a reader of the commit would want them.
                return files
                    .OrderBy(x => x.FlowName, StringComparer.OrdinalIgnoreCase)
                    .ThenBy(x => x.IsImage)
                    .ThenBy(x => x.Path, StringComparer.OrdinalIgnoreCase)
                    .ToList();
            }
            finally
            {
                _gate.Release();
            }
        }

        public async Task<RepositoryFlowChangesDto?> GetFlowChangesAsync(int flowId, CancellationToken ct)
        {
            Flow? flow = await FlowNameAsync(flowId, ct);
            if (flow == null)
                return null;

            string flowFileName = FileNameHelper.Clean(flow.Name, "flow");
            string scriptFile = RepositoryPathHelper.ScriptFileOf(flowFileName);

            await _gate.WaitAsync(ct);
            try
            {
                List<GitFileChange> changes = _git.Changes(_folderPath)
                    .Where(x => RepositoryPathHelper.BelongsTo(x.Path, flowFileName))
                    .ToList();

                byte[]? script = await ReadDiskAsync(scriptFile, ct);
                byte[]? committed = _git.ReadFile(_folderPath, null, scriptFile);

                List<RepositoryImageChangeDto> images = new List<RepositoryImageChangeDto>();
                foreach (GitFileChange change in changes.Where(x => RepositoryPathHelper.IsImage(x.Path)))
                {
                    RepositoryImageChangeDto image = new RepositoryImageChangeDto { Name = Path.GetFileName(change.Path), Kind = change.Kind };

                    if (change.Kind != GitChangeKindEnum.ADDED)
                        image.Before = _git.ReadFile(_folderPath, null, change.Path);

                    if (change.Kind != GitChangeKindEnum.DELETED)
                        image.After = await ReadDiskAsync(change.Path, ct);

                    images.Add(image);
                }

                return new RepositoryFlowChangesDto
                {
                    FlowId = flowId,
                    ScriptPath = scriptFile,
                    ChangeCount = changes.Count,
                    Script = Text(script) ?? string.Empty,
                    CommittedScript = Text(committed),
                    Images = images,
                };
            }
            finally
            {
                _gate.Release();
            }
        }

        public async Task<ResultDto<RepositoryCommitDto>> CommitAsync(RepositoryCommitRequestDto dto, CancellationToken ct)
        {
            string message = dto.Message.Trim();
            if (message.Length == 0)
                return ResultDto<RepositoryCommitDto>.Failure("A commit needs a message: say what changed.");

            if (dto.Paths.Count == 0)
                return ResultDto<RepositoryCommitDto>.Failure("Pick at least one file to commit.");

            if (!dto.Paths.All(RepositoryPathHelper.IsInside))
                return ResultDto<RepositoryCommitDto>.Failure("Only files inside the repository can be committed.");

            GitSignature author = await AuthorAsync(ct);
            GitCommit commit;

            await _gate.WaitAsync(ct);
            try
            {
                commit = _git.Commit(_folderPath, dto.Paths, message, author);
            }
            finally
            {
                _gate.Release();
            }

            RepositoryChangedDto changed = new RepositoryChangedDto();
            await _broadcast.SendAsync(BroadcastTypeEnum.REPOSITORY_CHANGED, changed);

            return ResultDto<RepositoryCommitDto>.Success(ToDto(commit));
        }

        public async Task<List<RepositoryCommitDto>> GetFlowHistoryAsync(int flowId, CancellationToken ct)
        {
            Flow? flow = await FlowNameAsync(flowId, ct);
            if (flow == null)
                return [];

            string flowFileName = FileNameHelper.Clean(flow.Name, "flow");
            string scriptFile = RepositoryPathHelper.ScriptFileOf(flowFileName);

            await _gate.WaitAsync(ct);
            try
            {
                List<GitCommit> commits = _git.Log(_folderPath, [scriptFile, flowFileName], HISTORY_LIMIT);

                return commits.Select(ToDto).ToList();
            }
            finally
            {
                _gate.Release();
            }
        }

        public async Task<GitFlowVersion> GetFlowVersionAsync(int flowId, CancellationToken ct)
        {
            List<int> flowIds = await WithSubFlowsAsync(flowId, ct);

            // Written now rather than once the saves go quiet, so a save a moment ago counts.
            await WriteAsync(flowIds, ct);

            List<Flow> flows = await FlowNamesAsync(ct);
            List<string> flowFileNames = flows
                .Where(x => flowIds.Contains(x.Id))
                .Select(x => FileNameHelper.Clean(x.Name, "flow"))
                .ToList();

            await _gate.WaitAsync(ct);
            try
            {
                GitCommit? last = _git.Log(_folderPath, [], 1).FirstOrDefault();
                bool hasUncommittedChanges = _git.Changes(_folderPath).Any(x => flowFileNames.Any(y => RepositoryPathHelper.BelongsTo(x.Path, y)));

                return new GitFlowVersion { CommitSha = last?.Sha, HasUncommittedChanges = hasUncommittedChanges };
            }
            finally
            {
                _gate.Release();
            }
        }

        public async Task<GitFlowFiles?> ReadFlowAtAsync(int flowId, string? commitSha, CancellationToken ct)
        {
            Flow? flow = await FlowNameAsync(flowId, ct);
            if (flow == null)
                return null;

            string flowFileName = FileNameHelper.Clean(flow.Name, "flow");
            string currentScriptFile = RepositoryPathHelper.ScriptFileOf(flowFileName);

            await _gate.WaitAsync(ct);
            try
            {
                List<string> files = _git.Files(_folderPath, commitSha);

                string? scriptFile = ScriptFileIn(files, commitSha, currentScriptFile, flow.PublicId);
                if (scriptFile == null)
                    return null;

                return ReadFlowFiles(files, commitSha, scriptFile);
            }
            finally
            {
                _gate.Release();
            }
        }

        public async Task<RepositoryFlowCommitDto?> GetFlowCommitAsync(RepositoryFlowCommitRequestDto dto, CancellationToken ct)
        {
            Flow? flow = await FlowNameAsync(dto.FlowId, ct);
            if (flow == null)
                return null;

            string flowFileName = FileNameHelper.Clean(flow.Name, "flow");
            string scriptFile = RepositoryPathHelper.ScriptFileOf(flowFileName);

            await _gate.WaitAsync(ct);
            try
            {
                GitCommit? commit = _git.FindCommit(_folderPath, dto.Sha);
                if (commit == null)
                    return null;

                byte[]? before = null;
                if (commit.ParentSha != null)
                    before = _git.ReadFile(_folderPath, commit.ParentSha, scriptFile);

                byte[]? after = _git.ReadFile(_folderPath, commit.Sha, scriptFile);

                List<RepositoryImageChangeDto> images = new List<RepositoryImageChangeDto>();
                foreach (GitFileChange change in _git.CommitChanges(_folderPath, commit.Sha, [flowFileName]).Where(x => RepositoryPathHelper.IsImage(x.Path)))
                {
                    RepositoryImageChangeDto image = new RepositoryImageChangeDto { Name = Path.GetFileName(change.Path), Kind = change.Kind };

                    if (change.Kind != GitChangeKindEnum.ADDED && commit.ParentSha != null)
                        image.Before = _git.ReadFile(_folderPath, commit.ParentSha, change.Path);

                    if (change.Kind != GitChangeKindEnum.DELETED)
                        image.After = _git.ReadFile(_folderPath, commit.Sha, change.Path);

                    images.Add(image);
                }

                return new RepositoryFlowCommitDto
                {
                    Commit = ToDto(commit),
                    ScriptBefore = Text(before),
                    ScriptAfter = Text(after),
                    Images = images,
                };
            }
            finally
            {
                _gate.Release();
            }
        }


        // ================================================================
        // Public methods - branches
        // ================================================================

        public async Task<List<RepositoryBranchDto>> GetBranchesAsync(CancellationToken ct)
        {
            await _gate.WaitAsync(ct);
            try
            {
                List<GitBranch> local = _git.Branches(_folderPath);
                List<RepositoryBranchDto> branches = local
                    .Select(x => new RepositoryBranchDto { Name = x.Name, IsCurrent = x.IsCurrent, LastCommitAt = x.When })
                    .ToList();

                // One only the remote has is made here by switching to it.
                foreach (GitBranch remote in _git.RemoteBranches(_folderPath).Where(x => !local.Any(y => string.Equals(y.Name, x.Name, StringComparison.OrdinalIgnoreCase))))
                    branches.Add(new RepositoryBranchDto { Name = remote.Name, LastCommitAt = remote.When, IsRemoteOnly = true });

                return branches;
            }
            finally
            {
                _gate.Release();
            }
        }

        public async Task<ResultDto<RepositoryBranchDto>> CreateBranchAsync(string name, CancellationToken ct)
        {
            string branch = name.Trim();
            if (branch.Length == 0)
                return ResultDto<RepositoryBranchDto>.Failure("A branch needs a name.");

            if (!_git.IsValidBranchName(branch))
                return ResultDto<RepositoryBranchDto>.Failure("Git does not take that as a branch name: no spaces, none of ~ ^ : ? * [ \\, and no two dots in a row.");

            GitBranch created;

            await _gate.WaitAsync(ct);
            try
            {
                List<GitBranch> branches = _git.Branches(_folderPath);
                GitBranch? current = branches.FirstOrDefault(x => x.IsCurrent);
                if (current == null)
                    return ResultDto<RepositoryBranchDto>.Failure("A branch starts from a commit, and there is none yet.");

                // Git keeps a branch as a file, and Windows and macOS ignore case.
                GitBranch? taken = branches.FirstOrDefault(x => string.Equals(x.Name, branch, StringComparison.OrdinalIgnoreCase));
                if (taken != null)
                    return ResultDto<RepositoryBranchDto>.Failure($"There is already a branch called \"{taken.Name}\".");

                // At the last commit, so nothing differs and the flows stay as they are, changes included.
                _git.CreateBranch(_folderPath, branch, current.Sha);
                _git.SwitchBranch(_folderPath, branch);

                created = _git.Branches(_folderPath).First(x => x.Name == branch);
            }
            finally
            {
                _gate.Release();
            }

            await BroadcastAsync([]);

            RepositoryBranchDto dto = new RepositoryBranchDto { Name = created.Name, IsCurrent = created.IsCurrent, LastCommitAt = created.When };
            return ResultDto<RepositoryBranchDto>.Success(dto);
        }

        public async Task<ResultDto<GitBranchSwitch>> SwitchBranchAsync(string branch, Func<GitBranchSwitch, CancellationToken, Task<ResultDto<bool>>> applyAsync, CancellationToken ct)
        {
            await WriteEveryFlowAsync(ct);

            await _gate.WaitAsync(ct);
            try
            {
                string? uncommitted = UncommittedRefusal();
                if (uncommitted != null)
                    return ResultDto<GitBranchSwitch>.Failure(uncommitted);

                List<GitBranch> branches = _git.Branches(_folderPath);
                GitBranch? from = branches.FirstOrDefault(x => x.IsCurrent);
                GitBranch? to = branches.FirstOrDefault(x => x.Name == branch);

                bool isRemoteOnly = false;
                if (to == null)
                {
                    to = _git.RemoteBranches(_folderPath).FirstOrDefault(x => x.Name == branch);
                    isRemoteOnly = to != null;
                }

                if (from == null || to == null)
                    return ResultDto<GitBranchSwitch>.Failure($"There is no branch called \"{branch}\".");

                GitBranchSwitch branchSwitch = ReadSwitch(from, to);
                if (to.IsCurrent)
                    return ResultDto<GitBranchSwitch>.Success(branchSwitch);

                // One only the remote has is made here, at the remote's commit, and goes again when
                // the switch does not happen.
                if (isRemoteOnly)
                    _git.CreateBranch(_folderPath, to.Name, to.Sha);

                _git.SwitchBranch(_folderPath, to.Name);

                return await ApplyOrUndoAsync(branchSwitch, applyAsync, () => UndoSwitch(from, to, isRemoteOnly), ct);
            }
            finally
            {
                _gate.Release();
            }
        }


        // ================================================================
        // Public methods - the remote
        // ================================================================

        public async Task<ResultDto<RepositoryConnectDto>> ConnectAsync(RepositoryConnectRequestDto dto, CancellationToken ct)
        {
            string url = dto.Url.Trim();
            string? refused = RefusedAddress(url);
            if (refused != null)
                return ResultDto<RepositoryConnectDto>.Failure(refused);

            string newToken = dto.Token.Trim();
            if (newToken.Length > 0)
                await _appSettingService.SetAsync(AppSettingKeyEnum.GIT_REMOTE_TOKEN, newToken, ct);

            string token = await TokenAsync(ct);
            string? previousUrl;

            await _gate.WaitAsync(ct);
            try
            {
                previousUrl = _git.RemoteUrl(_folderPath);
                _git.SetRemoteUrl(_folderPath, url);
            }
            finally
            {
                _gate.Release();
            }

            string? unreachable = OverTheNetwork(() => _git.Fetch(_folderPath, token));
            if (unreachable != null)
            {
                // An address that does not answer is not kept.
                await _gate.WaitAsync(ct);
                try
                {
                    _git.SetRemoteUrl(_folderPath, previousUrl);
                }
                finally
                {
                    _gate.Release();
                }

                return ResultDto<RepositoryConnectDto>.Failure(unreachable);
            }

            RepositoryConnectDto connected = new RepositoryConnectDto { RemoteUrl = url };
            List<string> branches;

            await _gate.WaitAsync(ct);
            try
            {
                branches = _git.Branches(_folderPath).Select(x => x.Name).ToList();
                if (_git.RemoteBranches(_folderPath).Count > 0)
                {
                    connected.Pull = ReadPull();
                    branches = [];
                }
            }
            finally
            {
                _gate.Release();
            }

            // An empty remote is sent every branch, history and all.
            if (branches.Count > 0)
            {
                string? notSent = OverTheNetwork(() => _git.Push(_folderPath, branches, token));
                if (notSent != null)
                    return ResultDto<RepositoryConnectDto>.Failure($"Connected, but the branches were not sent: {notSent} Push sends them.");

                connected.PushedBranches = branches;
            }

            await BroadcastAsync([]);
            return ResultDto<RepositoryConnectDto>.Success(connected);
        }

        public async Task<ResultDto<RepositoryPullDto>> PreviewPullAsync(CancellationToken ct)
        {
            string token = await TokenAsync(ct);
            if (!await HasRemoteAsync(ct))
                return ResultDto<RepositoryPullDto>.Failure(NO_REMOTE);

            string? unreachable = OverTheNetwork(() => _git.Fetch(_folderPath, token));
            if (unreachable != null)
                return ResultDto<RepositoryPullDto>.Failure(unreachable);

            RepositoryPullDto? pull;
            string branch;

            await _gate.WaitAsync(ct);
            try
            {
                pull = ReadPull();
                branch = _git.CurrentBranch(_folderPath);
            }
            finally
            {
                _gate.Release();
            }

            // What it brings shows in the status too.
            await BroadcastAsync([]);

            if (pull == null)
                return ResultDto<RepositoryPullDto>.Failure($"The remote has no branch {branch} yet. Push sends it.");

            return ResultDto<RepositoryPullDto>.Success(pull);
        }

        public async Task<ResultDto<GitBranchSwitch>> PullAsync(string remoteSha, Func<GitBranchSwitch, CancellationToken, Task<ResultDto<bool>>> applyAsync, CancellationToken ct)
        {
            await WriteEveryFlowAsync(ct);

            await _gate.WaitAsync(ct);
            try
            {
                string? uncommitted = UncommittedRefusal();
                if (uncommitted != null)
                    return ResultDto<GitBranchSwitch>.Failure(uncommitted);

                List<GitBranch> branches = _git.Branches(_folderPath);
                GitBranch? from = branches.FirstOrDefault(x => x.IsCurrent);
                if (from == null)
                    return ResultDto<GitBranchSwitch>.Failure("There is no commit to pull onto yet.");

                GitBranch? remote = _git.RemoteBranches(_folderPath).FirstOrDefault(x => x.Name == from.Name);
                if (remote == null)
                    return ResultDto<GitBranchSwitch>.Failure($"The remote has no branch {from.Name}.");

                // What the person agreed to drop was read against that commit.
                if (remote.Sha != remoteSha)
                    return ResultDto<GitBranchSwitch>.Failure("The remote has moved since. Pull again to see what it brings now.");

                GitBranchSwitch branchSwitch = ReadSwitch(from, remote);

                // Commits never pushed leave the branch, so they are kept on a branch of their own first.
                if (_git.CommitsBetween(_folderPath, from.Sha, remote.Sha).Count > 0)
                {
                    branchSwitch.BackupBranch = BackupNameFor(branches);
                    _git.CreateBranch(_folderPath, branchSwitch.BackupBranch, from.Sha);
                }

                _git.Reset(_folderPath, remote.Sha);

                return await ApplyOrUndoAsync(branchSwitch, applyAsync, () => UndoPull(from, branchSwitch.BackupBranch), ct);
            }
            finally
            {
                _gate.Release();
            }
        }

        public async Task<ResultDto<RepositoryPushDto>> PushAsync(CancellationToken ct)
        {
            string token = await TokenAsync(ct);
            if (!await HasRemoteAsync(ct))
                return ResultDto<RepositoryPushDto>.Failure(NO_REMOTE);

            // Fetched first, so what the remote has that the branch does not is known.
            string? unreachable = OverTheNetwork(() => _git.Fetch(_folderPath, token));
            if (unreachable != null)
                return ResultDto<RepositoryPushDto>.Failure(unreachable);

            GitBranch? local;
            int sending;

            await _gate.WaitAsync(ct);
            try
            {
                local = _git.Branches(_folderPath).FirstOrDefault(x => x.IsCurrent);
                if (local == null)
                    return ResultDto<RepositoryPushDto>.Failure("There is nothing to push: commit first.");

                GitBranch? remote = _git.RemoteBranches(_folderPath).FirstOrDefault(x => x.Name == local.Name);

                // Pushing over them would lose them. A pull brings them, and keeps what was never pushed.
                int behind = 0;
                if (remote != null)
                    behind = _git.CommitsBetween(_folderPath, remote.Sha, local.Sha).Count;

                if (behind > 0)
                {
                    string commits = behind == 1 ? "1 commit" : $"{behind} commits";
                    return ResultDto<RepositoryPushDto>.Failure($"The remote has {commits} {local.Name} does not. Pull first: it brings them, and keeps what was never pushed on a backup branch.");
                }

                sending = _git.CommitsBetween(_folderPath, local.Sha, remote?.Sha).Count;
            }
            finally
            {
                _gate.Release();
            }

            if (sending > 0)
            {
                string branch = local.Name;
                string? notSent = OverTheNetwork(() => _git.Push(_folderPath, [branch], token));
                if (notSent != null)
                    return ResultDto<RepositoryPushDto>.Failure(notSent);

                await BroadcastAsync([]);
            }

            return ResultDto<RepositoryPushDto>.Success(new RepositoryPushDto { Branch = local.Name, CommitCount = sending });
        }


        // ================================================================
        // Private methods - writing
        // ================================================================

        // Saves arrive in bursts - a form, the tree reloading, the thumbnail after it - so a flow is
        // written once they stop rather than once per save.
        private async Task<HashSet<int>> NextBatchAsync(CancellationToken ct)
        {
            HashSet<int> flowIds = new HashSet<int>();

            int first = await _changes.Reader.ReadAsync(ct);
            flowIds.Add(first);

            bool arrived = true;
            while (arrived)
            {
                await Task.Delay(QUIET, _timeProvider, ct);

                arrived = false;
                while (_changes.Reader.TryRead(out int flowId))
                {
                    flowIds.Add(flowId);
                    arrived = true;
                }
            }

            return flowIds;
        }

        // The export writes only what changed and removes the images the flow no longer uses. A
        // flow a branch switch hid since the list was read has no files on this branch.
        private async Task WriteFlowAsync(int flowId, CancellationToken ct)
        {
            await _gate.WaitAsync(ct);
            try
            {
                if (await IsVisibleAsync(flowId, ct))
                    await _exporter.ExportAsync(flowId, _folderPath, ct);
            }
            finally
            {
                _gate.Release();
            }
        }

        // Before the flows are written, and by exact name, case included. A renamed flow's files go
        // whole and are written again under the new name: on Windows and macOS, writing login.sflw
        // over Login.sflw would keep the old casing, and neither git nor Linux would see the rename.
        //
        // Nothing is removed while the database has no flows at all: a database that did not open
        // as expected must not empty the folder.
        private void RemoveGoneFlows(List<Flow> flows)
        {
            if (flows.Count == 0)
                return;

            HashSet<string> scriptFiles = flows
                .Select(x => FileNameHelper.Clean(x.Name, "flow"))
                .Select(RepositoryPathHelper.ScriptFileOf)
                .ToHashSet(StringComparer.Ordinal);

            string pattern = "*" + RepositoryPathHelper.SCRIPT_EXTENSION;
            List<string> paths = Directory.EnumerateFiles(_folderPath, pattern, SearchOption.TopDirectoryOnly).ToList();

            foreach (string path in paths)
            {
                string file = Path.GetFileName(path);
                if (scriptFiles.Contains(file) || !file.EndsWith(RepositoryPathHelper.SCRIPT_EXTENSION, StringComparison.OrdinalIgnoreCase))
                    continue;

                File.Delete(path);

                // No flow's folder starts with a dot, so a .git.sflw put here by hand never takes .git with it.
                string flowFileName = Path.GetFileNameWithoutExtension(path);
                string templateFolder = Path.Combine(_folderPath, flowFileName);
                if (!flowFileName.StartsWith('.') && Directory.Exists(templateFolder))
                    Directory.Delete(templateFolder, recursive: true);
            }
        }

        private ValueTask BroadcastAsync(IEnumerable<int> flowIds)
        {
            RepositoryChangedDto changed = new RepositoryChangedDto { FlowIds = flowIds.ToList() };

            return _broadcast.SendAsync(BroadcastTypeEnum.REPOSITORY_CHANGED, changed);
        }


        // ================================================================
        // Private methods
        // ================================================================

        // The Settings fields, else the user's git config.
        private async Task<GitSignature> AuthorAsync(CancellationToken ct)
        {
            string name = (await _appSettingService.GetTextAsync(AppSettingCatalog.GitAuthorName, ct)).Trim();
            string email = (await _appSettingService.GetTextAsync(AppSettingCatalog.GitAuthorEmail, ct)).Trim();

            if (name.Length == 0 || email.Length == 0)
            {
                GitSignature? configured = _git.ConfiguredAuthor(_folderPath);

                if (name.Length == 0)
                    name = configured?.Name ?? string.Empty;

                if (email.Length == 0)
                    email = configured?.Email ?? string.Empty;
            }

            // Somebody, at least: a commit cannot be by nobody.
            if (name.Length == 0)
                name = Environment.UserName;

            return new GitSignature { Name = name, Email = email, When = _timeProvider.GetLocalNow() };
        }

        // The flow and every flow it calls, however deep, since they all execute with it.
        private async Task<List<int>> WithSubFlowsAsync(int flowId, CancellationToken ct)
        {
            await using AppDbContext dbContext = await _dbContextFactory.CreateDbContextAsync(ct);

            HashSet<int> found = new HashSet<int> { flowId };
            List<int> callers = [flowId];

            while (callers.Count > 0)
            {
                List<int> called = await dbContext.FlowSteps
                    .AsNoTracking()
                    .Where(x => x.SubFlowId != null && callers.Contains(x.RootId))
                    .Select(x => x.SubFlowId!.Value)
                    .Distinct()
                    .ToListAsync(ct);

                callers = new List<int>();
                foreach (int subFlowId in called)
                {
                    if (found.Add(subFlowId))
                        callers.Add(subFlowId);
                }
            }

            return found.ToList();
        }

        private async Task<List<Flow>> FlowNamesAsync(CancellationToken ct)
        {
            await using AppDbContext dbContext = await _dbContextFactory.CreateDbContextAsync(ct);

            return await dbContext.Flows
                .AsNoTracking()
                .Where(x => !x.IsHidden)
                .Select(x => new Flow { Id = x.Id, Name = x.Name })
                .ToListAsync(ct);
        }

        private async Task<bool> IsVisibleAsync(int flowId, CancellationToken ct)
        {
            await using AppDbContext dbContext = await _dbContextFactory.CreateDbContextAsync(ct);

            return await dbContext.Flows.AnyAsync(x => x.Id == flowId && !x.IsHidden, ct);
        }

        private async Task<Flow?> FlowNameAsync(int flowId, CancellationToken ct)
        {
            await using AppDbContext dbContext = await _dbContextFactory.CreateDbContextAsync(ct);

            return await dbContext.Flows
                .AsNoTracking()
                .Where(x => x.Id == flowId)
                .Select(x => new Flow { Id = x.Id, Name = x.Name, PublicId = x.PublicId })
                .FirstOrDefaultAsync(ct);
        }

        // The file of the flow's name now first. One renamed since the commit is found by the Id in
        // its header, the one thing every copy of a flow keeps.
        private string? ScriptFileIn(List<string> files, string? commitSha, string currentScriptFile, Guid publicId)
        {
            List<string> scriptFiles = files
                .Where(x => !x.Contains('/', StringComparison.Ordinal) && x.EndsWith(RepositoryPathHelper.SCRIPT_EXTENSION, StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(x => x == currentScriptFile)
                .ToList();

            foreach (string scriptFile in scriptFiles)
            {
                byte[]? bytes = _git.ReadFile(_folderPath, commitSha, scriptFile);
                if (bytes == null)
                    continue;

                string script = Encoding.UTF8.GetString(bytes);
                if (IdOf(script) == publicId)
                    return scriptFile;
            }

            return null;
        }

        // A save not written yet would not show as a change, and a switch or a pull would replace it unseen.
        private async Task WriteEveryFlowAsync(CancellationToken ct)
        {
            List<Flow> flows = await FlowNamesAsync(ct);
            await WriteAsync(flows.Select(x => x.Id).ToList(), ct);
        }

        // A switch or a pull replaces flows, so what is not committed would be lost unseen.
        private string? UncommittedRefusal()
        {
            int changeCount = _git.Changes(_folderPath).Count;
            if (changeCount == 0)
                return null;

            string changes = changeCount == 1 ? "1 change" : $"{changeCount} changes";
            return $"Commit or discard the {changes} first: switching or pulling brings every flow to another version.";
        }

        // Called holding the folder, already moved. The database follows in applyAsync; when it does
        // not, undo puts the folder back to match the flows it kept.
        private static async Task<ResultDto<GitBranchSwitch>> ApplyOrUndoAsync(GitBranchSwitch branchSwitch, Func<GitBranchSwitch, CancellationToken, Task<ResultDto<bool>>> applyAsync, Action undo, CancellationToken ct)
        {
            bool isApplied = false;
            try
            {
                ResultDto<bool> applied = await applyAsync(branchSwitch, ct);
                if (!applied.IsSuccess)
                    return ResultDto<GitBranchSwitch>.Failure(applied.ErrorMessage ?? "The flows could not be imported.");

                isApplied = true;
                return ResultDto<GitBranchSwitch>.Success(branchSwitch);
            }
            finally
            {
                if (!isApplied)
                    undo();
            }
        }

        private void UndoSwitch(GitBranch from, GitBranch to, bool isRemoteOnly)
        {
            _git.SwitchBranch(_folderPath, from.Name);

            if (isRemoteOnly)
                _git.DeleteBranch(_folderPath, to.Name);
        }

        private void UndoPull(GitBranch from, string? backupBranch)
        {
            _git.Reset(_folderPath, from.Sha);

            if (backupBranch != null)
                _git.DeleteBranch(_folderPath, backupBranch);
        }

        // backup/2026-10-25-1530, with the seconds when two pulls share a minute.
        private string BackupNameFor(List<GitBranch> branches)
        {
            DateTimeOffset now = _timeProvider.GetLocalNow();

            string name = BACKUP_PREFIX + now.ToString("yyyy-MM-dd-HHmm", CultureInfo.InvariantCulture);
            if (branches.Any(x => x.Name == name))
                name = BACKUP_PREFIX + now.ToString("yyyy-MM-dd-HHmmss", CultureInfo.InvariantCulture);

            return name;
        }

        // What pulling the branch checked out would do, from what was last fetched: null when the
        // remote does not have the branch.
        private RepositoryPullDto? ReadPull()
        {
            GitBranch? local = _git.Branches(_folderPath).FirstOrDefault(x => x.IsCurrent);
            if (local == null)
                return null;

            GitBranch? remote = _git.RemoteBranches(_folderPath).FirstOrDefault(x => x.Name == local.Name);
            if (remote == null)
                return null;

            return new RepositoryPullDto
            {
                Branch = local.Name,
                RemoteSha = remote.Sha,
                Incoming = _git.CommitsBetween(_folderPath, remote.Sha, local.Sha).Select(ToDto).ToList(),
                Dropped = _git.CommitsBetween(_folderPath, local.Sha, remote.Sha).Select(ToDto).ToList(),
            };
        }

        private async Task<bool> HasRemoteAsync(CancellationToken ct)
        {
            await _gate.WaitAsync(ct);
            try
            {
                return _git.RemoteUrl(_folderPath) != null;
            }
            finally
            {
                _gate.Release();
            }
        }

        private async Task<string> TokenAsync(CancellationToken ct)
        {
            string token = await _appSettingService.GetTextAsync(AppSettingCatalog.GitRemoteToken, ct);

            return token.Trim();
        }

        // Outside the lock. The message to show when the remote cannot be reached or refuses.
        private static string? OverTheNetwork(Action action)
        {
            try
            {
                action();
                return null;
            }
            catch (InvalidOperationException exception)
            {
                return exception.Message;
            }
        }

        // HTTPS only: LibGit2Sharp speaks no SSH, and a token sent over plain HTTP goes in the clear.
        // A password in the address would be kept in the repository's config, in the clear too.
        private static string? RefusedAddress(string url)
        {
            if (url.StartsWith("git@", StringComparison.OrdinalIgnoreCase) || url.StartsWith("ssh://", StringComparison.OrdinalIgnoreCase))
                return "An SSH address is refused: the app reaches a remote over HTTPS, with a token. Copy the HTTPS address from the provider's page.";

            if (!Uri.TryCreate(url, UriKind.Absolute, out Uri? uri) || uri.Scheme != Uri.UriSchemeHttps || uri.Host.Length == 0)
                return "The address has to start with https://, as https://github.com/you/flows.git does.";

            if (uri.UserInfo.Contains(':', StringComparison.Ordinal))
                return "Put the token in its own field, not in the address.";

            return null;
        }

        // A flow's script and the images in its folder, as a commit holds them.
        private GitFlowFiles ReadFlowFiles(List<string> files, string? commitSha, string scriptFile)
        {
            byte[] script = _git.ReadFile(_folderPath, commitSha, scriptFile) ?? [];
            GitFlowFiles found = new GitFlowFiles { Path = scriptFile, Script = Encoding.UTF8.GetString(script) };

            string flowFileName = RepositoryPathHelper.FlowFileNameOf(scriptFile);
            foreach (string path in files.Where(x => RepositoryPathHelper.IsImage(x) && RepositoryPathHelper.BelongsTo(x, flowFileName)))
            {
                byte[]? image = _git.ReadFile(_folderPath, commitSha, path);
                if (image != null)
                    found.Templates[Path.GetFileName(path)] = image;
            }

            return found;
        }

        // Every flow whose files differ between the two branches, as the one switched to has it,
        // and the Id of each flow it does not have: one whose file it lacks, or whose file there
        // is another flow's now.
        private GitBranchSwitch ReadSwitch(GitBranch from, GitBranch to)
        {
            GitBranchSwitch branchSwitch = new GitBranchSwitch { From = from.Name, To = to.Name };
            if (from.Sha == to.Sha)
                return branchSwitch;

            List<string> flowFileNames = _git.Compare(_folderPath, from.Sha, to.Sha)
                .Select(x => RepositoryPathHelper.FlowFileNameOf(x.Path))
                .Where(x => x.Length > 0)
                .Distinct(StringComparer.Ordinal)
                .ToList();

            List<string> files = _git.Files(_folderPath, to.Sha);
            HashSet<Guid> fromIds = new HashSet<Guid>();
            HashSet<Guid> toIds = new HashSet<Guid>();

            foreach (string flowFileName in flowFileNames)
            {
                string scriptFile = RepositoryPathHelper.ScriptFileOf(flowFileName);

                byte[]? fromScript = _git.ReadFile(_folderPath, from.Sha, scriptFile);
                Guid? fromId = IdOf(Text(fromScript));
                if (fromId != null)
                    fromIds.Add(fromId.Value);

                if (!files.Contains(scriptFile))
                    continue;

                GitFlowFiles flow = ReadFlowFiles(files, to.Sha, scriptFile);
                branchSwitch.Flows.Add(flow);

                Guid? toId = IdOf(flow.Script);
                if (toId != null)
                    toIds.Add(toId.Value);
            }

            branchSwitch.GoneFlowIds = fromIds.Except(toIds).ToList();
            return branchSwitch;
        }

        // The header's Id: line, read without the scanner: which flow a file is, nothing more.
        private static Guid? IdOf(string? script)
        {
            if (script == null)
                return null;

            foreach (string line in script.Split('\n'))
            {
                string trimmed = line.Trim();
                if (!trimmed.StartsWith(ID_FIELD, StringComparison.Ordinal))
                    continue;

                string value = trimmed[ID_FIELD.Length..].Trim();
                if (Guid.TryParse(value, out Guid id))
                    return id;

                return null;
            }

            return null;
        }

        private async Task<byte[]?> ReadDiskAsync(string path, CancellationToken ct)
        {
            string fullPath = Path.Combine(_folderPath, path);
            if (!File.Exists(fullPath))
                return null;

            return await File.ReadAllBytesAsync(fullPath, ct);
        }

        private static string? Text(byte[]? bytes)
        {
            if (bytes == null)
                return null;

            return Encoding.UTF8.GetString(bytes);
        }

        private static RepositoryCommitDto ToDto(GitCommit commit)
        {
            return new RepositoryCommitDto
            {
                Sha = commit.Sha,
                ShortSha = commit.Sha[..Math.Min(SHORT_SHA_LENGTH, commit.Sha.Length)],
                Message = commit.Message,
                AuthorName = commit.AuthorName,
                When = commit.When,
            };
        }
    }
}
