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

            return new GitRepositoryService(_database, exporter, _git, new FakeAppSettingService(), _broadcast, TimeProvider.System, NullLogger<GitRepositoryService>.Instance, _path);
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
