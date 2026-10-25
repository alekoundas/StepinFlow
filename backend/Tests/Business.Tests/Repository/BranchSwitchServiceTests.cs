using System.Text;

using Business.Flows.DataService;
using Business.FlowScript;
using Business.FlowScript.Scanner;
using Business.FlowScript.Text;
using Business.Repository;
using Business.Tests.Fakes;
using Core.Enums;
using Core.Models.Business.Git;
using Core.Models.Database;
using Core.Models.Dtos;
using Core.Models.Dtos.Repository;
using DataAccess;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using static Business.Tests.TestToken;

namespace Business.Tests.Repository
{
    /// <summary>
    /// Branch switches in a real folder over a real database. A branch is the files the fake git was
    /// told its last commit holds: the flows written as one branch has them, kept, then changed.
    /// </summary>
    public sealed class BranchSwitchServiceTests : IDisposable
    {
        private static readonly byte[] PICTURE = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 1];

        private readonly TestDatabase _database = new TestDatabase();
        private readonly string _path = Path.Combine(Path.GetTempPath(), "stepinflow-branch-" + Guid.NewGuid().ToString("N")[..8]);
        private readonly FakeGitService _git = new FakeGitService { PendingChanges = [] };
        private readonly FakeExecutionEngine _engine = new FakeExecutionEngine();
        private readonly GitRepositoryService _repository;

        public BranchSwitchServiceTests()
        {
            Directory.CreateDirectory(_path);

            Printer printer = new Printer();
            FlowScriptExporter exporter = new FlowScriptExporter(_database, printer);
            _repository = new GitRepositoryService(_database, exporter, _git, new FakeAppSettingService(), new FakeIpcBroadcastService(), TimeProvider.System, NullLogger<GitRepositoryService>.Instance, _path);
        }

        public void Dispose()
        {
            _repository.Dispose();
            _database.Dispose();

            if (Directory.Exists(_path))
                Directory.Delete(_path, recursive: true);
        }

        private BranchSwitchService Service()
        {
            DataService data = TestDataService.For(_database);
            FlowScriptImporter importer = new FlowScriptImporter(new Scanner(), data);

            return new BranchSwitchService(_database, data, importer, _repository, _engine, new FakeIpcBroadcastService());
        }

        private Task<ResultDto<RepositoryBranchSwitchDto>> SwitchAsync(string branch)
        {
            return Service().SwitchAsync(branch, Ct);
        }

        private Task<ResultDto<RepositoryBranchSwitchDto>> PullAsync(string remoteSha)
        {
            return Service().PullAsync(remoteSha, Ct);
        }

        // The remote's branch, as the one here has it to start with.
        private Dictionary<string, byte[]> RemoteCopyOf(string branch)
        {
            Dictionary<string, byte[]> remote = _git.RemoteFiles(branch);
            foreach ((string path, byte[] bytes) in _git.BranchFiles(branch))
                remote[path] = bytes;

            return remote;
        }

        // A flow with one image search.
        private int AddFlow(string name, bool isSubFlow = false)
        {
            using AppDbContext db = _database.CreateDbContext();

            Flow flow = new Flow { Name = name, PublicId = Guid.NewGuid(), IsSubFlow = isSubFlow };
            db.Flows.Add(flow);
            db.SaveChanges();

            FlowStep find = new FlowStep { FlowStepType = FlowStepTypeEnum.SEARCH_IMAGE, SearchMode = SearchModeEnum.FIND_BEST, Name = "Find button", RootId = flow.Id, FlowId = flow.Id };
            db.FlowSteps.Add(find);
            db.SaveChanges();

            FlowTemplate template = new FlowTemplate { FlowId = flow.Id, Name = "template-a1b2c.png", TemplateImage = PICTURE, ClickOffsetX = 1, ClickOffsetY = 1 };
            db.FlowStepTemplates.Add(new FlowStepTemplate { FlowStepId = find.Id, FlowTemplate = template, OrderNumber = 0, Accuracy = 0.9f });
            db.SaveChanges();

            return flow.Id;
        }

        private void Change(int flowId, Action<AppDbContext, Flow> change)
        {
            using AppDbContext db = _database.CreateDbContext();

            Flow flow = db.Flows.Single(x => x.Id == flowId);
            change(db, flow);
            db.SaveChanges();
        }

        private Flow FlowOf(int flowId)
        {
            using AppDbContext db = _database.CreateDbContext();

            return db.Flows.AsNoTracking().Single(x => x.Id == flowId);
        }

        private string StepNameOf(int flowId)
        {
            using AppDbContext db = _database.CreateDbContext();

            return db.FlowSteps.Single(x => x.RootId == flowId && x.FlowStepType == FlowStepTypeEnum.SEARCH_IMAGE).Name;
        }

        // Every visible flow written, then the folder kept as the branch's last commit.
        private async Task KeepAsBranchAsync(string branch)
        {
            List<int> flowIds;
            using (AppDbContext db = _database.CreateDbContext())
                flowIds = db.Flows.Where(x => !x.IsHidden).Select(x => x.Id).ToList();

            await _repository.WriteAsync(flowIds, Ct);

            Dictionary<string, byte[]> files = _git.BranchFiles(branch);
            files.Clear();

            foreach (string path in Directory.EnumerateFiles(_path, "*", SearchOption.AllDirectories))
                files[Path.GetRelativePath(_path, path).Replace('\\', '/')] = await File.ReadAllBytesAsync(path, Ct);
        }

        private static void Remove(Dictionary<string, byte[]> files, string flowFileName)
        {
            foreach (string path in files.Keys.Where(x => x == flowFileName + ".sflw" || x.StartsWith(flowFileName + "/", StringComparison.Ordinal)).ToList())
                files.Remove(path);
        }

        private static void Edit(Dictionary<string, byte[]> files, string path, string oldText, string newText)
        {
            string script = Encoding.UTF8.GetString(files[path]);
            files[path] = Encoding.UTF8.GetBytes(script.Replace(oldText, newText, StringComparison.Ordinal));
        }


        [Fact]
        public async Task A_switch_imports_what_the_branch_changed_and_hides_what_it_does_not_have()
        {
            int login = AddFlow("Login");
            int report = AddFlow("Report");
            int checkout = AddFlow("Checkout");
            await KeepAsBranchAsync("feature");

            Dictionary<string, byte[]> feature = _git.BranchFiles("feature");
            Remove(feature, "Report");
            Edit(feature, "Login.sflw", "Find button", "Find the button");

            Change(checkout, (db, flow) => db.Flows.Remove(flow));
            await KeepAsBranchAsync("main");

            ResultDto<RepositoryBranchSwitchDto> switched = await SwitchAsync("feature");

            switched.IsSuccess.ShouldBeTrue(switched.ErrorMessage);
            _git.Current.ShouldBe("feature");
            StepNameOf(login).ShouldBe("Find the button");
            FlowOf(report).IsHidden.ShouldBeTrue();
            File.Exists(Path.Combine(_path, "Report.sflw")).ShouldBeFalse();

            using AppDbContext db = _database.CreateDbContext();
            Flow added = db.Flows.Single(x => x.Name == "Checkout");
            added.IsHidden.ShouldBeFalse();

            switched.Data.ShouldNotBeNull().Flows.Select(x => (x.Name, x.Change, x.IsDone)).ShouldBe(
            [
                ("Report", GitChangeKindEnum.DELETED, true),
                ("Checkout", GitChangeKindEnum.ADDED, true),
                ("Login", GitChangeKindEnum.MODIFIED, true),
            ]);
        }

        // Hidden, never deleted: the same row comes back, its executions with it.
        [Fact]
        public async Task A_flow_hidden_by_one_switch_comes_back_with_its_executions_on_the_next()
        {
            AddFlow("Login");
            int report = AddFlow("Report");
            using (AppDbContext db = _database.CreateDbContext())
            {
                db.Executions.Add(new Execution { FlowId = report, Status = ExecutionStatusEnum.COMPLETED });
                db.SaveChanges();
            }

            await KeepAsBranchAsync("main");
            Dictionary<string, byte[]> feature = _git.BranchFiles("feature");
            foreach ((string path, byte[] bytes) in _git.Committed)
                feature[path] = bytes;

            Remove(feature, "Report");

            (await SwitchAsync("feature")).IsSuccess.ShouldBeTrue();
            ResultDto<RepositoryBranchSwitchDto> back = await SwitchAsync("main");

            back.IsSuccess.ShouldBeTrue(back.ErrorMessage);
            FlowOf(report).IsHidden.ShouldBeFalse();
            File.Exists(Path.Combine(_path, "Report.sflw")).ShouldBeTrue();

            using AppDbContext context = _database.CreateDbContext();
            context.Executions.Count(x => x.FlowId == report).ShouldBe(1);
        }

        // Login.sflw comes before Pay.sflw, so in the order the files are read the call would find nothing.
        [Fact]
        public async Task A_sub_flow_new_on_the_branch_is_imported_before_the_flow_that_calls_it()
        {
            int login = AddFlow("Login");
            await KeepAsBranchAsync("main");

            int pay = AddFlow("Pay", isSubFlow: true);
            using (AppDbContext db = _database.CreateDbContext())
            {
                db.FlowSteps.Add(new FlowStep { FlowStepType = FlowStepTypeEnum.SUB_FLOW, Name = "Pay up", SubFlowId = pay, RootId = login, FlowId = login, OrderNumber = 1 });
                db.SaveChanges();
            }

            await KeepAsBranchAsync("feature");

            Change(login, (db, flow) => db.FlowSteps.RemoveRange(db.FlowSteps.Where(x => x.FlowStepType == FlowStepTypeEnum.SUB_FLOW)));
            Change(pay, (db, flow) => db.Flows.Remove(flow));
            await _repository.WriteAsync([login], Ct);

            ResultDto<RepositoryBranchSwitchDto> switched = await SwitchAsync("feature");

            switched.IsSuccess.ShouldBeTrue(switched.ErrorMessage);
            switched.Data.ShouldNotBeNull().Flows.Select(x => x.Name).ShouldBe(["Pay", "Login"]);

            using AppDbContext context = _database.CreateDbContext();
            int newPay = context.Flows.Single(x => x.Name == "Pay").Id;
            context.FlowSteps.Single(x => x.RootId == login && x.FlowStepType == FlowStepTypeEnum.SUB_FLOW).SubFlowId.ShouldBe(newPay);
        }

        [Fact]
        public async Task Two_flows_the_branch_swapped_the_names_of_do_not_refuse_each_other()
        {
            int alpha = AddFlow("Alpha");
            int beta = AddFlow("Beta");
            await KeepAsBranchAsync("main");

            Change(alpha, (_, flow) => flow.Name = "Gamma");
            Change(beta, (_, flow) => flow.Name = "Alpha");
            Change(alpha, (_, flow) => flow.Name = "Beta");
            await KeepAsBranchAsync("feature");
            _git.Current = "feature";

            ResultDto<RepositoryBranchSwitchDto> switched = await SwitchAsync("main");

            switched.IsSuccess.ShouldBeTrue(switched.ErrorMessage);
            (FlowOf(alpha).Name, FlowOf(beta).Name).ShouldBe(("Alpha", "Beta"));
        }

        [Fact]
        public async Task A_file_that_does_not_read_refuses_the_switch_and_leaves_everything_as_it_was()
        {
            int login = AddFlow("Login");
            AddFlow("Report");
            await KeepAsBranchAsync("main");
            byte[] script = _git.Committed["Login.sflw"];

            Dictionary<string, byte[]> feature = _git.BranchFiles("feature");
            foreach ((string path, byte[] bytes) in _git.Committed)
                feature[path] = bytes;

            Edit(feature, "Login.sflw", "Find button", "Find the button");
            feature["Report.sflw"] = Encoding.UTF8.GetBytes("This is not a flow.");

            ResultDto<RepositoryBranchSwitchDto> switched = await SwitchAsync("feature");

            switched.IsSuccess.ShouldBeFalse();
            switched.ErrorMessage.ShouldNotBeNull().ShouldContain("Report.sflw");
            _git.Switches.ShouldBe(["feature", "main"]);
            StepNameOf(login).ShouldBe("Find button");
            (await File.ReadAllBytesAsync(Path.Combine(_path, "Login.sflw"), Ct)).ShouldBe(script);
        }

        [Fact]
        public async Task A_switch_is_refused_while_anything_is_not_committed()
        {
            AddFlow("Login");
            await KeepAsBranchAsync("main");
            _git.CreateBranch(_path, "feature", _git.ShaOf("main"));
            _git.PendingChanges = [new GitFileChange { Path = "Login.sflw", Kind = GitChangeKindEnum.MODIFIED }];

            ResultDto<RepositoryBranchSwitchDto> switched = await SwitchAsync("feature");

            switched.IsSuccess.ShouldBeFalse();
            switched.ErrorMessage.ShouldNotBeNull().ShouldContain("Commit or discard the 1 change");
            _git.Switches.ShouldBeEmpty();
        }

        [Fact]
        public async Task A_pull_brings_the_remotes_flows_and_keeps_what_was_never_pushed_on_a_backup_branch()
        {
            int login = AddFlow("Login");
            int report = AddFlow("Report");
            await KeepAsBranchAsync("main");
            string before = _git.ShaOf("main");

            Dictionary<string, byte[]> remote = RemoteCopyOf("main");
            Remove(remote, "Report");
            Edit(remote, "Login.sflw", "Find button", "Find the button");
            _git.Between = (sha, except) => sha == before ? [new GitCommit { Sha = "local", Message = "Never pushed" }] : [];

            ResultDto<RepositoryBranchSwitchDto> pulled = await PullAsync(_git.RemoteShaOf("main"));

            pulled.IsSuccess.ShouldBeTrue(pulled.ErrorMessage);
            StepNameOf(login).ShouldBe("Find the button");
            FlowOf(report).IsHidden.ShouldBeTrue();
            _git.ShaOf("main").ShouldBe(_git.RemoteShaOf("main"));

            string backup = pulled.Data.ShouldNotBeNull().BackupBranch.ShouldNotBeNull();
            backup.ShouldStartWith("backup/");
            _git.ShaOf(backup).ShouldBe(before);
        }

        [Fact]
        public async Task A_pull_that_does_not_import_puts_the_branch_back_and_keeps_no_backup()
        {
            AddFlow("Login");
            await KeepAsBranchAsync("main");
            string before = _git.ShaOf("main");
            byte[] script = _git.Committed["Login.sflw"];

            Dictionary<string, byte[]> remote = RemoteCopyOf("main");
            remote["Login.sflw"] = Encoding.UTF8.GetBytes("This is not a flow.");
            _git.Between = (sha, except) => sha == before ? [new GitCommit { Sha = "local", Message = "Never pushed" }] : [];

            ResultDto<RepositoryBranchSwitchDto> pulled = await PullAsync(_git.RemoteShaOf("main"));

            pulled.IsSuccess.ShouldBeFalse();
            _git.ShaOf("main").ShouldBe(before);
            _git.Branches(_path).ShouldAllBe(x => !x.Name.StartsWith("backup/", StringComparison.Ordinal));
            (await File.ReadAllBytesAsync(Path.Combine(_path, "Login.sflw"), Ct)).ShouldBe(script);
        }

        // What the person agreed to drop was read against the commit the dialog showed.
        [Fact]
        public async Task A_pull_is_refused_when_the_remote_has_moved_since_it_was_shown()
        {
            AddFlow("Login");
            await KeepAsBranchAsync("main");
            string before = _git.ShaOf("main");
            RemoteCopyOf("main");

            ResultDto<RepositoryBranchSwitchDto> pulled = await PullAsync("an-older-commit");

            pulled.IsSuccess.ShouldBeFalse();
            pulled.ErrorMessage.ShouldNotBeNull().ShouldContain("moved");
            _git.ShaOf("main").ShouldBe(before);
        }

        [Fact]
        public async Task A_branch_only_the_remote_has_is_made_here_by_switching_to_it()
        {
            int login = AddFlow("Login");
            await KeepAsBranchAsync("main");

            Dictionary<string, byte[]> remote = RemoteCopyOf("main");
            _git.RemoteFiles("login-fix");
            foreach ((string path, byte[] bytes) in remote)
                _git.RemoteFiles("login-fix")[path] = bytes;

            Edit(_git.RemoteFiles("login-fix"), "Login.sflw", "Find button", "Find the button");

            ResultDto<RepositoryBranchSwitchDto> switched = await SwitchAsync("login-fix");

            switched.IsSuccess.ShouldBeTrue(switched.ErrorMessage);
            _git.Current.ShouldBe("login-fix");
            _git.ShaOf("login-fix").ShouldBe(_git.RemoteShaOf("login-fix"));
            StepNameOf(login).ShouldBe("Find the button");
        }

        [Fact]
        public async Task A_switch_is_refused_while_a_flow_executes()
        {
            _git.CreateBranch(_path, "feature", _git.ShaOf("main"));
            _engine.IsRunning = true;

            ResultDto<RepositoryBranchSwitchDto> switched = await SwitchAsync("feature");

            switched.IsSuccess.ShouldBeFalse();
            _git.Switches.ShouldBeEmpty();
        }
    }
}
