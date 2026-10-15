using System.Drawing;
using Business.Flows.DataService;
using Business.FlowScript;
using Business.FlowScript.Scanner;
using Business.FlowScript.Text;
using Business.Recording.Draft;
using Business.Recording.Generation;
using Business.Recording.Setup;
using Business.Tests.Fakes;
using Core.Enums;
using Core.Models.Business;
using Core.Models.Business.AreaPointResolution;
using Core.Models.Database;
using Core.Models.Dtos;
using Core.Models.Dtos.FlowScript;
using DataAccess;
using Microsoft.EntityFrameworkCore;
using static Business.Tests.TestToken;

namespace Business.Tests.Recording
{
    /// <summary>
    /// The plain draft, end to end: a flow made by the recording setup, a reviewed recording added
    /// to it, written as a script and imported through a real database - or inserted at a position,
    /// as the editor's "Record steps here" does.
    /// </summary>
    public sealed class FlowGenerationServiceTests : IDisposable
    {
        private const int CHROME = 9;

        private readonly TestDatabase _database = new TestDatabase();
        private readonly FakeAreaPointResolver _resolver = new FakeAreaPointResolver();
        private readonly FakeWindowService _windows = new FakeWindowService();
        private readonly FakeProcessService _processes = new FakeProcessService();
        private readonly DataService _dataService;
        private readonly RecordingSetupService _setup;
        private readonly FlowGenerationService _generation;

        private readonly RecordingResultDto _recording = new RecordingResultDto();
        private readonly ReviewedRecordingDto _reviewed = new ReviewedRecordingDto();

        public FlowGenerationServiceTests()
        {
            _dataService = TestDataService.For(_database);
            _setup = new RecordingSetupService(_dataService, _resolver, _windows, _processes);

            FlowScriptExporter exporter = new FlowScriptExporter(_database, new Printer());
            FlowScriptImporter importer = new FlowScriptImporter(new Scanner(), _dataService);
            RecordingDraftBuilder draftBuilder = new RecordingDraftBuilder(new FakeScreenshotService());
            _generation = new FlowGenerationService(exporter, importer, draftBuilder, new Printer(), new Scanner(), _dataService);

            _resolver.AreasByName["chrome"] = AreaResolution.Ok(new Rectangle(0, 0, 800, 600), new WindowHandle(7));
            _windows.WindowProcessId = CHROME;
            _processes.ExecutablePaths[CHROME] = @"C:\Program Files\Google\Chrome\Application\chrome.exe";
        }

        public void Dispose()
        {
            _database.Dispose();
        }

        private async Task<int> NewFlowAsync()
        {
            RecordingSetupDto setup = new RecordingSetupDto
            {
                Name = "Download invoices",
                Description = "Logs in and downloads last month's invoices.",
                IsQaTest = true,
                ProcessName = "chrome",
                ScalesWith = ScalesWithEnum.DPI,
                AddsLaunchStep = true,
            };

            ResultDto<int> created = await _setup.CreateFlowAsync(setup, Ct);
            created.IsSuccess.ShouldBeTrue(created.ErrorMessage);

            return created.Data;
        }

        private void Record()
        {
            _recording.Actions.Add(new RecordedActionDto { Index = 0, Kind = RecordedActionKindEnum.CLICK, LocationX = 400, LocationY = 300, ScreenshotIndex = 0, WaitedMilliseconds = 1200, Window = RecordedWindowEnum.MAIN_AREA });
            _recording.Actions.Add(new RecordedActionDto { Index = 1, Kind = RecordedActionKindEnum.TYPING, Text = "ops@example.com" });
            _recording.ClickedElements.Add(new ClickedElementDto { ActionIndex = 0, X = 380, Y = 290, Width = 40, Height = 20, Text = "Email", FoundBy = ElementFoundByEnum.MODEL, Image = [2] });

            _reviewed.Actions.Add(new ReviewedActionDto { ActionIndex = 0, Choice = ReviewChoiceEnum.WAIT_AND_CLICK });
            _reviewed.Actions.Add(new ReviewedActionDto { ActionIndex = 1, Choice = ReviewChoiceEnum.INPUT, InputName = "email" });
        }

        private static RecordingScreenDto ScreenAt(int actionIndex)
        {
            return new RecordingScreenDto { ActionIndex = 0, Image = [1], X = 0, Y = 0, Width = 800, Height = 600, Dpi = 96 };
        }

        [Fact]
        public async Task A_reviewed_recording_becomes_its_flow_after_the_launch_step()
        {
            int flowId = await NewFlowAsync();
            Record();

            FlowScriptImportResultDto imported = await _generation.ImportPlainDraftAsync(flowId, _recording, _reviewed, ScreenAt, Ct);

            imported.IsSuccess.ShouldBeTrue();
            imported.Errors.ShouldBeEmpty();
            imported.FlowId.ShouldBe(flowId);
            imported.MissingTemplates.ShouldBeEmpty();

            using AppDbContext db = _database.CreateDbContext();
            List<FlowStep> roots = await db.FlowSteps.Where(x => x.RootId == flowId && x.ParentFlowStepId == null).OrderBy(x => x.OrderNumber).ToListAsync(Ct);
            roots.Select(x => x.FlowStepType).ShouldBe([FlowStepTypeEnum.SYSTEM_COMMAND, FlowStepTypeEnum.SEARCH_IMAGE]);
            roots[1].Name.ShouldBe("Find Email");

            FlowStepTemplate template = await db.FlowStepTemplates.SingleAsync(x => x.FlowStepId == roots[1].Id, Ct);
            template.TemplateImage.ShouldNotBeNull();

            FlowCsvColumn input = await db.FlowCsvColumns.SingleAsync(x => x.FlowId == flowId, Ct);
            (input.Name, input.DefaultValue).ShouldBe(("email", "ops@example.com"));
        }

        // The import replaces the flow's rows and keeps the row itself, so what the setup said stays.
        [Fact]
        public async Task The_flow_keeps_what_the_setup_said_about_it()
        {
            int flowId = await NewFlowAsync();
            Record();

            await _generation.ImportPlainDraftAsync(flowId, _recording, _reviewed, ScreenAt, Ct);

            using AppDbContext db = _database.CreateDbContext();
            Flow flow = await db.Flows.SingleAsync(Ct);
            (flow.Id, flow.Name, flow.IsQaTest).ShouldBe((flowId, "Download invoices", true));
            flow.Description.ShouldBe("Logs in and downloads last month's invoices.");
            (await db.FlowAreas.SingleAsync(Ct)).IsMain.ShouldBeTrue();
        }

        // ================================================================
        // Into a flow that already existed
        // ================================================================

        // Only the new rows are written, so a step already there keeps its history.
        [Fact]
        public async Task A_recording_at_a_position_goes_in_between_and_the_steps_already_there_keep_their_ids()
        {
            int flowId = await NewFlowAsync();
            int launchId = Steps(flowId).Single(x => x.ParentFlowStepId == null).Id;
            Record();

            RecordingDraftPositionDto position = new RecordingDraftPositionDto { FlowId = flowId, Index = 0 };
            ResultDto<FlowScriptImportResultDto> inserted = await _generation.InsertPlainDraftAsync(position, _recording, _reviewed, ScreenAt, Ct);

            inserted.IsSuccess.ShouldBeTrue(inserted.ErrorMessage);
            FlowScriptImportResultDto result = inserted.Data.ShouldNotBeNull();
            (result.IsSuccess, result.FlowId).ShouldBe((true, flowId));

            List<FlowStep> steps = Steps(flowId);
            List<FlowStep> roots = steps.Where(x => x.ParentFlowStepId == null).OrderBy(x => x.OrderNumber).ToList();
            roots.Select(x => x.Name).ShouldBe(["Find Email", "Launch chrome"]);
            roots[1].Id.ShouldBe(launchId);

            using AppDbContext db = _database.CreateDbContext();
            FlowStepTemplate template = await db.FlowStepTemplates.SingleAsync(x => x.FlowStepId == roots[0].Id, Ct);
            template.TemplateImage.ShouldNotBeNull();
            FlowCsvColumn input = await db.FlowCsvColumns.SingleAsync(x => x.FlowId == flowId, Ct);
            (input.Name, input.DefaultValue).ShouldBe(("email", "ops@example.com"));
        }

        [Fact]
        public async Task A_recording_under_a_branch_goes_in_that_branch()
        {
            int flowId = await NewFlowAsync();
            FlowStep check = new FlowStep { RootId = flowId, FlowId = flowId, OrderNumber = 1, FlowStepType = FlowStepTypeEnum.SEARCH_IMAGE, Name = "Signed in" };
            int checkId = (await _dataService.FlowStep.CreateAsync(check, [], Ct)).Data;
            int successId = Steps(flowId).Single(x => x.ParentFlowStepId == checkId && x.FlowStepType == FlowStepTypeEnum.SUCCESS).Id;
            Record();

            RecordingDraftPositionDto position = new RecordingDraftPositionDto { FlowId = flowId, ParentFlowStepId = successId, Index = 0 };
            ResultDto<FlowScriptImportResultDto> inserted = await _generation.InsertPlainDraftAsync(position, _recording, _reviewed, ScreenAt, Ct);

            inserted.IsSuccess.ShouldBeTrue(inserted.ErrorMessage);
            Steps(flowId).Where(x => x.ParentFlowStepId == successId).Select(x => x.Name).ShouldBe(["Find Email"]);
        }

        [Fact]
        public async Task A_position_under_a_step_no_longer_there_writes_nothing_and_says_so()
        {
            int flowId = await NewFlowAsync();
            int stepCount = Steps(flowId).Count;
            Record();

            RecordingDraftPositionDto position = new RecordingDraftPositionDto { FlowId = flowId, ParentFlowStepId = 999, Index = 0 };
            ResultDto<FlowScriptImportResultDto> inserted = await _generation.InsertPlainDraftAsync(position, _recording, _reviewed, ScreenAt, Ct);

            inserted.ErrorMessage.ShouldBe("The step the recording goes under is no longer in the flow.");
            Steps(flowId).Count.ShouldBe(stepCount);
        }

        private List<FlowStep> Steps(int flowId)
        {
            using AppDbContext db = _database.CreateDbContext();
            return db.FlowSteps.AsNoTracking().Where(x => x.RootId == flowId).ToList();
        }
    }
}
