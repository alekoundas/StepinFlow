using Business.FlowScript;
using Business.FlowScript.Text;
using Business.Repository;
using Business.Tests.Fakes;
using Core.Enums;
using Core.Models.Business.Git;
using Core.Models.Database;
using DataAccess;
using Microsoft.Extensions.Logging.Abstractions;
using static Business.Tests.TestToken;

namespace Business.Tests.Repository
{
    /// <summary>
    /// The repository's folder kept a copy of the database, in a real folder over a real database.
    /// </summary>
    public sealed class GitRepositoryServiceWriteTests : IDisposable
    {
        private readonly TestDatabase _database = new TestDatabase();
        private readonly string _path = Path.Combine(Path.GetTempPath(), "stepinflow-repository-" + Guid.NewGuid().ToString("N")[..8]);
        private readonly FakeGitService _git = new FakeGitService();
        private readonly FakeIpcBroadcastService _broadcast = new FakeIpcBroadcastService();

        public GitRepositoryServiceWriteTests()
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

        // Polls, since the service writes on a loop of its own. Fails after five seconds.
        private static async Task EventuallyAsync(Func<bool> condition)
        {
            using CancellationTokenSource timeout = CancellationTokenSource.CreateLinkedTokenSource(Ct);
            timeout.CancelAfter(TimeSpan.FromSeconds(5));

            while (!condition())
                await Task.Delay(20, timeout.Token);
        }

        private bool HasBroadcast()
        {
            lock (_broadcast.Sent)
                return _broadcast.Sent.Count > 0;
        }

        private string PathOf(params string[] parts)
        {
            return Path.Combine([_path, .. parts]);
        }

        // A flow with one image search, looking for each template given.
        private int SeedFlow(string name, params (string Name, byte[] Image)[] templates)
        {
            using AppDbContext db = _database.CreateDbContext();

            Flow flow = new Flow { Name = name, PublicId = Guid.NewGuid() };
            db.Flows.Add(flow);
            db.SaveChanges();

            FlowStep find = new FlowStep { FlowStepType = FlowStepTypeEnum.SEARCH_IMAGE, SearchMode = SearchModeEnum.FIND_BEST, Name = "Find " + name, RootId = flow.Id, FlowId = flow.Id };
            db.FlowSteps.Add(find);
            db.SaveChanges();

            int order = 0;
            foreach ((string templateName, byte[] image) in templates)
            {
                FlowTemplate template = new FlowTemplate { FlowId = flow.Id, Name = templateName, TemplateImage = image, ClickOffsetX = 1, ClickOffsetY = 1 };
                db.FlowStepTemplates.Add(new FlowStepTemplate { FlowStepId = find.Id, FlowTemplate = template, OrderNumber = order++, Accuracy = 0.9f });
            }

            db.SaveChanges();
            return flow.Id;
        }

        private int SeedCaller(string name, int subFlowId)
        {
            using AppDbContext db = _database.CreateDbContext();

            Flow flow = new Flow { Name = name, PublicId = Guid.NewGuid() };
            db.Flows.Add(flow);
            db.SaveChanges();

            db.FlowSteps.Add(new FlowStep { FlowStepType = FlowStepTypeEnum.SUB_FLOW, Name = "Pay up", SubFlowId = subFlowId, RootId = flow.Id, FlowId = flow.Id });
            db.SaveChanges();

            return flow.Id;
        }

        private void Rename(int flowId, string name)
        {
            using AppDbContext db = _database.CreateDbContext();

            db.Flows.Single(x => x.Id == flowId).Name = name;
            db.SaveChanges();
        }

        private void Delete(int flowId)
        {
            using AppDbContext db = _database.CreateDbContext();

            db.Flows.Remove(db.Flows.Single(x => x.Id == flowId));
            db.SaveChanges();
        }


        // ================================================================
        // Writing a flow
        // ================================================================

        [Fact]
        public async Task A_flow_is_written_as_its_script_and_its_template_images()
        {
            byte[] picture = [1, 2, 3];
            int flowId = SeedFlow("Login", ("template-a1b2c.png", picture));

            await Service().WriteAsync([flowId], Ct);

            string script = await File.ReadAllTextAsync(PathOf("Login.sflw"), Ct);
            script.ShouldStartWith("Flow:");
            script.ShouldContain("template-a1b2c.png");
            script.ShouldNotContain("\r");
            (await File.ReadAllBytesAsync(PathOf("Login", "template-a1b2c.png"), Ct)).ShouldBe(picture);
        }

        [Fact]
        public async Task A_file_whose_bytes_have_not_changed_is_not_written_again()
        {
            int flowId = SeedFlow("Login", ("template-a1b2c.png", [1, 2, 3]));
            await Service().WriteAsync([flowId], Ct);

            DateTime longAgo = new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            File.SetLastWriteTimeUtc(PathOf("Login.sflw"), longAgo);
            File.SetLastWriteTimeUtc(PathOf("Login", "template-a1b2c.png"), longAgo);

            await Service().WriteAsync([flowId], Ct);

            File.GetLastWriteTimeUtc(PathOf("Login.sflw")).ShouldBe(longAgo);
            File.GetLastWriteTimeUtc(PathOf("Login", "template-a1b2c.png")).ShouldBe(longAgo);
        }

        [Fact]
        public async Task An_image_no_step_names_goes_and_anything_else_in_the_folder_stays()
        {
            int flowId = SeedFlow("Login", ("template-a1b2c.png", [1]));
            Directory.CreateDirectory(PathOf("Login"));
            await File.WriteAllBytesAsync(PathOf("Login", "template-gone1.png"), [9], Ct);
            await File.WriteAllTextAsync(PathOf("Login", "README.md"), "notes", Ct);

            await Service().WriteAsync([flowId], Ct);

            Directory.GetFiles(PathOf("Login")).Select(Path.GetFileName).Order().ShouldBe(["README.md", "template-a1b2c.png"]);
        }

        // ================================================================
        // A flow renamed or gone
        // ================================================================

        [Fact]
        public async Task A_renamed_flow_takes_its_files_with_it()
        {
            int flowId = SeedFlow("Login", ("template-a1b2c.png", [1]));
            await Service().WriteAsync([flowId], Ct);
            Rename(flowId, "Sign in");

            await Service().WriteAsync([flowId], Ct);

            File.Exists(PathOf("Login.sflw")).ShouldBeFalse();
            Directory.Exists(PathOf("Login")).ShouldBeFalse();
            File.Exists(PathOf("Sign in.sflw")).ShouldBeTrue();
            File.Exists(PathOf("Sign in", "template-a1b2c.png")).ShouldBeTrue();
        }

        [Fact]
        public async Task A_rename_that_only_changes_case_renames_the_files()
        {
            int flowId = SeedFlow("Login", ("template-a1b2c.png", [1]));
            await Service().WriteAsync([flowId], Ct);
            Rename(flowId, "login");

            await Service().WriteAsync([flowId], Ct);

            Directory.GetFiles(_path, "*.sflw").Select(Path.GetFileName).ShouldBe(["login.sflw"]);
            Directory.GetDirectories(_path).Select(Path.GetFileName).ShouldBe(["login"]);
        }

        // Whatever else was in it too: a folder kept for a README would keep its old casing.
        [Fact]
        public async Task A_renamed_flows_old_folder_goes_whole()
        {
            int flowId = SeedFlow("Login", ("template-a1b2c.png", [1]));
            await Service().WriteAsync([flowId], Ct);
            await File.WriteAllTextAsync(PathOf("Login", "README.md"), "notes", Ct);
            Rename(flowId, "login");

            await Service().WriteAsync([flowId], Ct);

            Directory.GetDirectories(_path).Select(Path.GetFileName).ShouldBe(["login"]);
            Directory.GetFiles(PathOf("login")).Select(Path.GetFileName).ShouldBe(["template-a1b2c.png"]);
        }

        [Fact]
        public async Task A_deleted_flow_loses_its_files_and_the_others_keep_theirs()
        {
            int login = SeedFlow("Login", ("template-a1b2c.png", [1]));
            int other = SeedFlow("Other");
            await Service().WriteAsync([login, other], Ct);
            Delete(login);

            await Service().WriteAsync([login], Ct);

            File.Exists(PathOf("Login.sflw")).ShouldBeFalse();
            Directory.Exists(PathOf("Login")).ShouldBeFalse();
            File.Exists(PathOf("Other.sflw")).ShouldBeTrue();
        }

        [Fact]
        public async Task A_flow_that_calls_a_renamed_sub_flow_is_written_again()
        {
            int subFlow = SeedFlow("Checkout");
            int caller = SeedCaller("Shop", subFlow);
            await Service().WriteAsync([subFlow, caller], Ct);
            Rename(subFlow, "Pay");

            await Service().WriteAsync([subFlow], Ct);

            string script = await File.ReadAllTextAsync(PathOf("Shop.sflw"), Ct);
            script.ShouldContain("<[ Pay ]>   id <[");
        }

        [Fact]
        public async Task A_database_with_no_flows_never_empties_the_folder()
        {
            await File.WriteAllTextAsync(PathOf("Old.sflw"), "Flow: Old\n", Ct);

            await Service().WriteAsync([1], Ct);

            File.Exists(PathOf("Old.sflw")).ShouldBeTrue();
        }

        // ================================================================
        // The version an execution starts at
        // ================================================================

        [Fact]
        public async Task A_flows_version_is_the_last_commit_and_counts_a_sub_flows_changes()
        {
            int checkout = SeedFlow("Checkout");
            int shop = SeedCaller("Shop", checkout);
            SeedFlow("Other");
            await Service().StartAsync(Ct);

            _git.PendingChanges = [new GitFileChange { Path = "Other.sflw", Kind = GitChangeKindEnum.MODIFIED }];
            GitFlowVersion clean = await Service().GetFlowVersionAsync(shop, Ct);

            _git.PendingChanges = [new GitFileChange { Path = "Checkout.sflw", Kind = GitChangeKindEnum.MODIFIED }];
            GitFlowVersion dirty = await Service().GetFlowVersionAsync(shop, Ct);

            (clean.CommitSha, clean.HasUncommittedChanges).ShouldBe((new string('a', 40), false));
            dirty.HasUncommittedChanges.ShouldBeTrue();
        }

        // Without waiting for the saves to go quiet, so a save a moment before Execute counts.
        [Fact]
        public async Task A_flows_files_are_written_before_its_version_is_read()
        {
            int flowId = SeedFlow("Login");

            GitFlowVersion version = await Service().GetFlowVersionAsync(flowId, Ct);

            File.Exists(PathOf("Login.sflw")).ShouldBeTrue();
            version.HasUncommittedChanges.ShouldBeTrue();
            version.CommitSha.ShouldBeNull();
        }

        // ================================================================
        // A save
        // ================================================================

        [Fact]
        public async Task A_flow_a_save_says_changed_is_written_once_the_saves_go_quiet()
        {
            _git.MakesRepository = false;
            using GitRepositoryService service = Service();
            using CancellationTokenSource stop = CancellationTokenSource.CreateLinkedTokenSource(Ct);

            // Started with no flows, so only the save can have written it.
            Task running = service.StartBackgroundService(stop.Token);
            await EventuallyAsync(HasBroadcast);

            int flowId = SeedFlow("Login");
            service.Changed(flowId);
            service.Changed(flowId);

            await EventuallyAsync(() => File.Exists(PathOf("Login.sflw")));

            await stop.CancelAsync();
            await running;
        }

        // ================================================================
        // Startup
        // ================================================================

        [Fact]
        public async Task A_new_repository_gets_every_flow_in_one_first_commit()
        {
            SeedFlow("Login", ("template-a1b2c.png", [1]));
            SeedFlow("Other");

            await Service().StartAsync(Ct);

            (IReadOnlyList<string> paths, string message, _) = _git.Commits.ShouldHaveSingleItem();
            message.ShouldBe("Snapshot of 2 flows");
            paths.ShouldBe([".gitattributes", ".gitignore", "Login.sflw", "Login/template-a1b2c.png", "Other.sflw"], ignoreOrder: true);
            (await File.ReadAllTextAsync(PathOf(".gitattributes"), Ct)).ShouldContain("*.sflw text eol=lf");
        }

        [Fact]
        public async Task A_repository_already_there_is_brought_up_to_date_and_nothing_committed()
        {
            _git.MakesRepository = false;
            SeedFlow("Login");

            await Service().StartAsync(Ct);

            _git.Commits.ShouldBeEmpty();
            File.Exists(PathOf("Login.sflw")).ShouldBeTrue();
        }

        [Fact]
        public async Task An_older_gitignore_is_put_back_as_the_app_writes_it()
        {
            _git.MakesRepository = false;
            await File.WriteAllTextAsync(PathOf(".gitignore"), "Thumbs.db\n", Ct);

            await Service().StartAsync(Ct);

            string ignore = await File.ReadAllTextAsync(PathOf(".gitignore"), Ct);
            ignore.ShouldStartWith("*\n");
            ignore.ShouldContain("!*.png\n");
        }
    }
}
