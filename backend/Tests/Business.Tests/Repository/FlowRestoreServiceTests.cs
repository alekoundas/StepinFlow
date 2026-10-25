using Business.FlowScript;
using Business.FlowScript.Scanner;
using Business.FlowScript.Text;
using Business.Repository;
using Business.Tests.Fakes;
using Core.Enums;
using Core.Models.Database;
using Core.Models.Dtos;
using Core.Models.Dtos.FlowScript;
using DataAccess;
using Microsoft.Extensions.Logging.Abstractions;
using static Business.Tests.TestToken;

namespace Business.Tests.Repository
{
    /// <summary>
    /// A flow brought back to a commit, in a real folder over a real database, the commit being the
    /// files the fake git was told it holds.
    /// </summary>
    public sealed class FlowRestoreServiceTests : IDisposable
    {
        private static readonly byte[] PICTURE = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 1];

        private readonly TestDatabase _database = new TestDatabase();
        private readonly string _path = Path.Combine(Path.GetTempPath(), "stepinflow-restore-" + Guid.NewGuid().ToString("N")[..8]);
        private readonly FakeGitService _git = new FakeGitService();
        private readonly GitRepositoryService _repository;

        public FlowRestoreServiceTests()
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

        private FlowRestoreService Restore()
        {
            FlowScriptImporter importer = new FlowScriptImporter(new Scanner(), TestDataService.For(_database));

            return new FlowRestoreService(_repository, importer, new FakeIpcBroadcastService());
        }

        private string PathOf(params string[] parts)
        {
            return Path.Combine([_path, .. parts]);
        }

        // A flow with one image search, written to the folder and committed as it is.
        private async Task<int> SeedCommittedFlowAsync(string name)
        {
            int flowId;
            using (AppDbContext db = _database.CreateDbContext())
            {
                Flow flow = new Flow { Name = name, PublicId = Guid.NewGuid() };
                db.Flows.Add(flow);
                db.SaveChanges();

                FlowStep find = new FlowStep { FlowStepType = FlowStepTypeEnum.SEARCH_IMAGE, SearchMode = SearchModeEnum.FIND_BEST, Name = "Find button", RootId = flow.Id, FlowId = flow.Id };
                db.FlowSteps.Add(find);
                db.SaveChanges();

                FlowTemplate template = new FlowTemplate { FlowId = flow.Id, Name = "template-a1b2c.png", TemplateImage = PICTURE, ClickOffsetX = 1, ClickOffsetY = 1 };
                db.FlowStepTemplates.Add(new FlowStepTemplate { FlowStepId = find.Id, FlowTemplate = template, OrderNumber = 0, Accuracy = 0.9f });
                db.SaveChanges();

                flowId = flow.Id;
            }

            await _repository.WriteAsync([flowId], Ct);

            foreach (string path in Directory.EnumerateFiles(_path, "*", SearchOption.AllDirectories))
                _git.Committed[Path.GetRelativePath(_path, path).Replace('\\', '/')] = await File.ReadAllBytesAsync(path, Ct);

            return flowId;
        }

        private void Change(int flowId, Action<AppDbContext, Flow> change)
        {
            using AppDbContext db = _database.CreateDbContext();

            Flow flow = db.Flows.Single(x => x.Id == flowId);
            change(db, flow);
            db.SaveChanges();
        }

        private (string FlowName, string StepName, byte[]? Picture) Read(int flowId)
        {
            using AppDbContext db = _database.CreateDbContext();

            string flowName = db.Flows.Single(x => x.Id == flowId).Name;
            FlowStep step = db.FlowSteps.Single(x => x.RootId == flowId && x.FlowStepType == FlowStepTypeEnum.SEARCH_IMAGE);
            byte[]? picture = db.FlowTemplates.Single(x => x.FlowId == flowId).TemplateImage;

            return (flowName, step.Name, picture);
        }


        [Fact]
        public async Task Discard_puts_the_flow_back_as_the_last_commit_has_it()
        {
            int flowId = await SeedCommittedFlowAsync("Login");
            byte[] committedScript = _git.Committed["Login.sflw"];
            Change(flowId, (db, flow) =>
            {
                db.FlowSteps.Single(x => x.RootId == flow.Id && x.FlowStepType == FlowStepTypeEnum.SEARCH_IMAGE).Name = "Find another button";
                db.FlowTemplates.Single(x => x.FlowId == flow.Id).TemplateImage = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 2];
            });

            ResultDto<FlowScriptImportResultDto> result = await Restore().DiscardAsync(flowId, Ct);

            result.IsSuccess.ShouldBeTrue(result.ErrorMessage);
            (string flowName, string stepName, byte[]? picture) = Read(flowId);
            (flowName, stepName).ShouldBe(("Login", "Find button"));
            picture.ShouldBe(PICTURE);
            (await File.ReadAllBytesAsync(PathOf("Login.sflw"), Ct)).ShouldBe(committedScript);
        }

        // Found by its Id, so the name it had at the commit comes back, files and all.
        [Fact]
        public async Task Discard_takes_back_a_rename_too()
        {
            int flowId = await SeedCommittedFlowAsync("Login");
            Change(flowId, (_, flow) => flow.Name = "Sign in");
            await _repository.WriteAsync([flowId], Ct);

            ResultDto<FlowScriptImportResultDto> result = await Restore().DiscardAsync(flowId, Ct);

            result.IsSuccess.ShouldBeTrue(result.ErrorMessage);
            Read(flowId).FlowName.ShouldBe("Login");
            File.Exists(PathOf("Login.sflw")).ShouldBeTrue();
            File.Exists(PathOf("Sign in.sflw")).ShouldBeFalse();
        }

        [Fact]
        public async Task A_flow_never_committed_has_nothing_to_go_back_to()
        {
            int flowId = await SeedCommittedFlowAsync("Login");
            _git.Committed.Clear();

            ResultDto<FlowScriptImportResultDto> discarded = await Restore().DiscardAsync(flowId, Ct);
            ResultDto<FlowScriptImportResultDto> restored = await Restore().RestoreAsync(flowId, new string('b', 40), Ct);

            discarded.IsSuccess.ShouldBeFalse();
            restored.IsSuccess.ShouldBeFalse();
            Read(flowId).StepName.ShouldBe("Find button");
        }
    }
}
