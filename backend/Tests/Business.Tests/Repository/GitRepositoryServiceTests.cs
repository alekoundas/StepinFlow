using Business.FlowScript;
using Business.FlowScript.Text;
using Business.Flows.DataService;
using Business.Repository;
using Business.Tests.Fakes;
using Core.Enums;
using Core.Models.Business.Git;
using Core.Models.Database;
using Core.Models.Dtos;
using Core.Models.Dtos.Repository;
using DataAccess;
using Microsoft.Extensions.Logging.Abstractions;
using static Business.Tests.TestToken;

namespace Business.Tests.Repository
{
    /// <summary>
    /// What the app is told about the repository, over a fake git, and what a save says changed.
    /// </summary>
    public sealed class GitRepositoryServiceTests : IDisposable
    {
        private readonly TestDatabase _database = new TestDatabase();
        private readonly string _path = Path.Combine(Path.GetTempPath(), "stepinflow-repository-" + Guid.NewGuid().ToString("N")[..8]);
        private readonly FakeGitService _git = new FakeGitService();
        private readonly FakeIpcBroadcastService _broadcast = new FakeIpcBroadcastService();
        private readonly FakeAppSettingService _settings = new FakeAppSettingService();

        public GitRepositoryServiceTests()
        {
            Directory.CreateDirectory(_path);
        }

        public void Dispose()
        {
            _database.Dispose();

            if (Directory.Exists(_path))
                Directory.Delete(_path, recursive: true);
        }

        private GitRepositoryService Service()
        {
            Printer printer = new Printer();
            FlowScriptExporter exporter = new FlowScriptExporter(_database, printer);

            return new GitRepositoryService(_database, exporter, _git, _settings, _broadcast, TimeProvider.System, NullLogger<GitRepositoryService>.Instance, _path);
        }

        private async Task<GitSignature> CommitAuthorAsync()
        {
            await Service().CommitAsync(new RepositoryCommitRequestDto { Message = "Fix the login", Paths = ["Login.sflw"] }, Ct);

            return _git.Commits.ShouldHaveSingleItem().Author;
        }

        private int SeedFlow(string name)
        {
            using AppDbContext db = _database.CreateDbContext();

            Flow flow = new Flow { Name = name, PublicId = Guid.NewGuid() };
            db.Flows.Add(flow);
            db.SaveChanges();

            return flow.Id;
        }

        private static GitFileChange Change(string path, GitChangeKindEnum kind)
        {
            return new GitFileChange { Path = path, Kind = kind };
        }

        private Task<ResultDto<RepositoryConnectDto>> ConnectAsync(string url, string token = "")
        {
            return Service().ConnectAsync(new RepositoryConnectRequestDto { Url = url, Token = token }, Ct);
        }

        private static GitCommit CommitNamed(string message)
        {
            return new GitCommit { Sha = message, Message = message };
        }


        // At the last commit, so nothing differs and nothing has to be brought across.
        [Fact]
        public async Task A_new_branch_starts_at_the_last_commit_and_is_checked_out()
        {
            _git.Committed["Login.sflw"] = [1];

            ResultDto<RepositoryBranchDto> created = await Service().CreateBranchAsync("  login-fix ", Ct);

            created.IsSuccess.ShouldBeTrue(created.ErrorMessage);
            created.Data.ShouldNotBeNull().Name.ShouldBe("login-fix");
            _git.Current.ShouldBe("login-fix");
            _git.Committed["Login.sflw"].ShouldBe([1]);
            _broadcast.Sent.ShouldContain(x => x.Type == BroadcastTypeEnum.REPOSITORY_CHANGED);
        }

        // HTTPS only, and the token in its own field rather than written into the repository's config.
        [Theory]
        [InlineData("git@github.com:you/flows.git")]
        [InlineData("http://github.com/you/flows.git")]
        [InlineData("github.com/you/flows")]
        [InlineData("https://you:secret@github.com/you/flows.git")]
        public async Task An_address_that_is_not_https_or_holds_a_password_is_refused(string url)
        {
            ResultDto<RepositoryConnectDto> connected = await ConnectAsync(url, "secret");

            connected.IsSuccess.ShouldBeFalse();
            _git.Remote.ShouldBeNull();
            _git.Tokens.ShouldBeEmpty();
        }

        [Fact]
        public async Task Connecting_to_an_empty_remote_sends_it_every_branch_signed_in_with_the_token()
        {
            _git.CreateBranch(_path, "login-fix", _git.ShaOf("main"));

            ResultDto<RepositoryConnectDto> connected = await ConnectAsync("https://github.com/you/flows.git", " secret ");

            connected.IsSuccess.ShouldBeTrue(connected.ErrorMessage);
            connected.Data.ShouldNotBeNull().PushedBranches.ShouldBe(["login-fix", "main"]);
            connected.Data.Pull.ShouldBeNull();
            _git.Remote.ShouldBe("https://github.com/you/flows.git");
            _git.Tokens.ShouldAllBe(x => x == "secret");
            _settings.Stored[AppSettingKeyEnum.GIT_REMOTE_TOKEN].ShouldBe("secret");
        }

        [Fact]
        public async Task An_address_that_does_not_answer_is_not_kept()
        {
            _git.Remote = "https://github.com/you/old.git";
            _git.FetchError = "There is no repository at that address, or the token cannot see it.";

            ResultDto<RepositoryConnectDto> connected = await ConnectAsync("https://github.com/you/flows.git");

            connected.IsSuccess.ShouldBeFalse();
            connected.ErrorMessage.ShouldBe(_git.FetchError);
            _git.Remote.ShouldBe("https://github.com/you/old.git");
            _git.Pushed.ShouldBeEmpty();
        }

        // Nothing is replaced yet: the person is shown what a pull would bring and drop first.
        [Fact]
        public async Task Connecting_to_a_remote_with_history_says_what_pulling_would_do()
        {
            _git.RemoteFiles("main")["Login.sflw"] = [1];
            _git.Between = (sha, except) => sha == _git.RemoteShaOf("main") ? [CommitNamed("Their change")] : [CommitNamed("Snapshot")];

            ResultDto<RepositoryConnectDto> connected = await ConnectAsync("https://github.com/you/flows.git");

            connected.IsSuccess.ShouldBeTrue(connected.ErrorMessage);
            _git.Pushed.ShouldBeEmpty();

            RepositoryPullDto pull = connected.Data.ShouldNotBeNull().Pull.ShouldNotBeNull();
            pull.RemoteSha.ShouldBe(_git.RemoteShaOf("main"));
            pull.Incoming.Select(x => x.Message).ShouldBe(["Their change"]);
            pull.Dropped.Select(x => x.Message).ShouldBe(["Snapshot"]);
        }

        [Fact]
        public async Task A_push_sends_the_branch_checked_out()
        {
            _git.Remote = "https://github.com/you/flows.git";
            _git.Between = (sha, except) => sha == _git.ShaOf("main") ? [CommitNamed("One"), CommitNamed("Two")] : [];

            ResultDto<RepositoryPushDto> pushed = await Service().PushAsync(Ct);

            pushed.IsSuccess.ShouldBeTrue(pushed.ErrorMessage);
            pushed.Data.ShouldNotBeNull().CommitCount.ShouldBe(2);
            _git.Pushed.ShouldBe(["main"]);
        }

        // Pushing over them would lose them; a pull brings them, and keeps what was never pushed.
        [Fact]
        public async Task A_push_is_refused_while_the_remote_has_commits_the_branch_does_not()
        {
            _git.Remote = "https://github.com/you/flows.git";
            _git.RemoteFiles("main");
            _git.Between = (sha, except) => sha == _git.RemoteShaOf("main") ? [CommitNamed("Their change")] : [];

            ResultDto<RepositoryPushDto> pushed = await Service().PushAsync(Ct);

            pushed.IsSuccess.ShouldBeFalse();
            pushed.ErrorMessage.ShouldNotBeNull().ShouldContain("Pull first");
            _git.Pushed.ShouldBeEmpty();
        }

        [Fact]
        public async Task The_status_says_how_far_the_branch_is_from_the_remote()
        {
            _git.Remote = "https://github.com/you/flows.git";
            _git.RemoteFiles("main");
            _git.Between = (sha, except) => sha == _git.ShaOf("main") ? [CommitNamed("Mine")] : [CommitNamed("Theirs"), CommitNamed("Theirs too")];

            RepositoryStatusDto status = await Service().GetStatusAsync(Ct);

            (status.RemoteUrl, status.IsBranchOnRemote, status.AheadCount, status.BehindCount).ShouldBe(("https://github.com/you/flows.git", true, 1, 2));
        }

        // Git keeps a branch as a file, so "MAIN" would be main on Windows and macOS.
        [Theory]
        [InlineData("")]
        [InlineData("login fix")]
        [InlineData("MAIN")]
        public async Task A_branch_name_git_does_not_take_or_one_taken_is_refused(string name)
        {
            ResultDto<RepositoryBranchDto> created = await Service().CreateBranchAsync(name, Ct);

            created.IsSuccess.ShouldBeFalse();
            _git.Current.ShouldBe("main");
            _git.Branches(_path).Count.ShouldBe(1);
        }

        [Fact]
        public async Task Each_change_belongs_to_the_flow_its_path_names()
        {
            int login = SeedFlow("Login");
            Directory.CreateDirectory(Path.Combine(_path, "Login"));
            await File.WriteAllBytesAsync(Path.Combine(_path, "Login", "template-a1b2c.png"), [7], Ct);

            _git.PendingChanges =
            [
                Change("Login/template-a1b2c.png", GitChangeKindEnum.ADDED),
                Change("Login.sflw", GitChangeKindEnum.MODIFIED),
                Change("Gone.sflw", GitChangeKindEnum.DELETED),
            ];

            List<RepositoryFileChangeDto> changes = await Service().GetChangesAsync(Ct);

            changes.Select(x => (x.Path, x.FlowId, x.FlowName)).ShouldBe(
            [
                ("Gone.sflw", (int?)null, "Gone"),
                ("Login.sflw", login, "Login"),
                ("Login/template-a1b2c.png", login, "Login"),
            ]);
            changes.Single(x => x.IsImage).Image.ShouldBe([7]);
        }

        [Fact]
        public async Task The_status_counts_each_flows_changes()
        {
            int login = SeedFlow("Login");
            SeedFlow("Other");

            _git.PendingChanges =
            [
                Change("Login.sflw", GitChangeKindEnum.MODIFIED),
                Change("Login/template-a1b2c.png", GitChangeKindEnum.ADDED),
                Change(".gitattributes", GitChangeKindEnum.MODIFIED),
            ];

            RepositoryStatusDto status = await Service().GetStatusAsync(Ct);

            status.Branch.ShouldBe("main");
            status.ChangeCount.ShouldBe(3);
            RepositoryFlowStatusDto changed = status.Flows.ShouldHaveSingleItem();
            (changed.FlowId, changed.ChangeCount).ShouldBe((login, 2));
        }

        [Fact]
        public async Task A_flows_changes_hold_its_script_then_and_now()
        {
            int login = SeedFlow("Login");
            await File.WriteAllTextAsync(Path.Combine(_path, "Login.sflw"), "Flow: Login\nnow\n", Ct);
            _git.Committed["Login.sflw"] = "Flow: Login\nthen\n"u8.ToArray();
            _git.PendingChanges = [Change("Login.sflw", GitChangeKindEnum.MODIFIED)];

            RepositoryFlowChangesDto? changes = await Service().GetFlowChangesAsync(login, Ct);

            changes.ShouldNotBeNull();
            changes.Script.ShouldBe("Flow: Login\nnow\n");
            changes.CommittedScript.ShouldBe("Flow: Login\nthen\n");
            changes.ChangeCount.ShouldBe(1);
        }

        [Theory]
        [InlineData("   ", "Login.sflw")]
        [InlineData("Fix the login", "")]
        [InlineData("Fix the login", "../outside.txt")]
        [InlineData("Fix the login", "C:/outside.txt")]
        public async Task A_commit_without_a_message_or_a_file_inside_the_repository_is_refused(string message, string path)
        {
            List<string> paths = path.Length == 0 ? [] : [path];

            ResultDto<RepositoryCommitDto> result = await Service().CommitAsync(new RepositoryCommitRequestDto { Message = message, Paths = paths }, Ct);

            result.IsSuccess.ShouldBeFalse();
            _git.Commits.ShouldBeEmpty();
        }

        [Fact]
        public async Task A_commit_takes_exactly_the_files_picked_and_says_so()
        {
            ResultDto<RepositoryCommitDto> result = await Service().CommitAsync(new RepositoryCommitRequestDto { Message = " Fix the login ", Paths = ["Login.sflw"] }, Ct);

            result.IsSuccess.ShouldBeTrue();
            (IReadOnlyList<string> paths, string message, _) = _git.Commits.ShouldHaveSingleItem();
            paths.ShouldBe(["Login.sflw"]);
            message.ShouldBe("Fix the login");
            _broadcast.Sent.ShouldContain(x => x.Type == BroadcastTypeEnum.REPOSITORY_CHANGED);
        }

        [Fact]
        public async Task The_author_comes_from_the_git_config_when_Settings_leave_it_empty()
        {
            _git.Configured = new GitSignature { Name = "Alex", Email = "alex@example.com" };

            GitSignature author = await CommitAuthorAsync();

            (author.Name, author.Email).ShouldBe(("Alex", "alex@example.com"));
        }

        [Fact]
        public async Task With_no_author_anywhere_a_commit_is_still_by_somebody()
        {
            GitSignature author = await CommitAuthorAsync();

            author.Name.ShouldBe(Environment.UserName);
        }

        // ================================================================
        // What a save says changed
        // ================================================================

        [Fact]
        public async Task Every_write_says_which_flow_it_changed_deletes_included()
        {
            FakeGitRepositoryService repository = new FakeGitRepositoryService();
            DataService data = TestDataService.For(_database, repository);

            ResultDto<int> flow = await data.Flow.CreateAsync(new Flow { Name = "Login" }, Ct);
            ResultDto<int> area = await data.FlowArea.CreateAsync(new FlowArea { Name = "Box", FlowId = flow.Data, Type = FlowAreaTypeEnum.CUSTOM }, Ct);
            ResultDto<int> input = await data.FlowCsvColumn.CreateAsync(new FlowCsvColumn { Name = "email", FlowId = flow.Data }, Ct);
            await data.FlowArea.DeleteAsync(area.Data, Ct);
            await data.FlowCsvColumn.DeleteAsync(input.Data, Ct);
            await data.Flow.DeleteAsync(flow.Data, Ct);

            repository.Changes.ShouldBe([flow.Data, flow.Data, flow.Data, flow.Data, flow.Data, flow.Data]);
        }
    }
}
