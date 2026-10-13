using System.Drawing;

using Business.Flows.DataService;
using Business.Recording.Setup;
using Business.Tests.Fakes;
using Core.Enums;
using Core.Models.Business;
using Core.Models.Business.AreaPointResolution;
using Core.Models.Database;
using Core.Models.Dtos;
using DataAccess;
using static Business.Tests.TestToken;

namespace Business.Tests.Recording
{
    /// <summary>
    /// The flow a recording goes into, made before the first click, and thrown away with the
    /// recording while nothing but the setup is in it.
    /// </summary>
    public sealed class RecordingSetupServiceTests : IDisposable
    {
        private const int CHROME = 9;
        private const string CHROME_PATH = @"C:\Program Files\Google\Chrome\Application\chrome.exe";

        private static readonly WindowHandle CHROME_WINDOW = new WindowHandle(7);

        private readonly TestDatabase _database = new TestDatabase();
        private readonly FakeAreaPointResolver _resolver = new FakeAreaPointResolver();
        private readonly FakeWindowService _windows = new FakeWindowService();
        private readonly FakeProcessService _processes = new FakeProcessService();
        private readonly DataService _dataService;
        private readonly RecordingSetupService _setup;

        public RecordingSetupServiceTests()
        {
            _dataService = TestDataService.For(_database);
            _setup = new RecordingSetupService(_dataService, _resolver, _windows, _processes);

            _resolver.AreasByName["chrome"] = AreaResolution.Ok(new Rectangle(0, 0, 800, 600), CHROME_WINDOW);
            _windows.WindowProcessId = CHROME;
            _processes.ExecutablePaths[CHROME] = CHROME_PATH;
        }

        public void Dispose()
        {
            _database.Dispose();
        }

        private static RecordingSetupDto Setup(bool addsLaunchStep = true)
        {
            return new RecordingSetupDto
            {
                Name = "Download invoices",
                Description = "Logs in and downloads this month's invoices.",
                IsQaTest = true,
                ProcessName = "chrome",
                ScalesWith = ScalesWithEnum.DPI,
                AddsLaunchStep = addsLaunchStep,
            };
        }

        private Flow Load(int flowId)
        {
            using AppDbContext db = _database.CreateDbContext();
            Flow flow = db.Flows.Single(x => x.Id == flowId);
            flow.FlowAreas = db.FlowAreas.Where(x => x.FlowId == flowId).ToList();
            flow.FlowSteps = db.FlowSteps.Where(x => x.RootId == flowId).ToList();
            return flow;
        }

        // ================================================================
        // Setting up
        // ================================================================

        [Fact]
        public async Task The_flow_its_main_area_and_a_step_launching_the_application_are_saved_together()
        {
            ResultDto<int> created = await _setup.CreateFlowAsync(Setup(), Ct);

            Flow flow = Load(created.Data);
            (flow.Name, flow.IsQaTest).ShouldBe(("Download invoices", true));

            FlowArea main = flow.FlowAreas.ShouldHaveSingleItem();
            (main.Name, main.IsMain, main.Type, main.ProcessName).ShouldBe(("chrome", true, FlowAreaTypeEnum.APPLICATION, "chrome"));

            FlowStep launch = flow.FlowSteps.Single(x => x.FlowStepType == FlowStepTypeEnum.SYSTEM_COMMAND);
            (launch.RunCommandPreset, launch.RunCommandValue, launch.OrderNumber).ShouldBe((RunCommandPresetEnum.LAUNCH_APP, $"\"{CHROME_PATH}\"", 0));
            flow.FlowSteps.Count(x => x.ParentFlowStepId == launch.Id).ShouldBe(2);
        }

        [Fact]
        public async Task Without_the_launch_step_the_flow_starts_empty()
        {
            ResultDto<int> created = await _setup.CreateFlowAsync(Setup(addsLaunchStep: false), Ct);

            Load(created.Data).FlowSteps.ShouldBeEmpty();
        }

        // The launch starts the program the window belongs to, so there has to be a window.
        [Fact]
        public async Task A_launch_step_for_an_application_that_is_not_open_is_refused_and_nothing_is_saved()
        {
            _resolver.AreasByName.Clear();

            ResultDto<int> created = await _setup.CreateFlowAsync(Setup(), Ct);

            created.IsSuccess.ShouldBeFalse();
            using AppDbContext db = _database.CreateDbContext();
            db.Flows.ShouldBeEmpty();
        }

        [Fact]
        public async Task A_name_another_flow_has_is_refused()
        {
            await _setup.CreateFlowAsync(Setup(), Ct);

            ResultDto<int> again = await _setup.CreateFlowAsync(Setup(), Ct);

            again.IsSuccess.ShouldBeFalse();
        }

        // ================================================================
        // Discarding
        // ================================================================

        [Fact]
        public async Task Discarding_deletes_a_flow_with_nothing_but_the_setup_in_it()
        {
            ResultDto<int> created = await _setup.CreateFlowAsync(Setup(), Ct);

            (await _dataService.Flow.DeleteRecordingSetupAsync(created.Data, Ct)).Data.ShouldBeTrue();

            using AppDbContext db = _database.CreateDbContext();
            db.Flows.ShouldBeEmpty();
        }

        [Fact]
        public async Task Discarding_keeps_a_flow_steps_were_added_to()
        {
            ResultDto<int> created = await _setup.CreateFlowAsync(Setup(), Ct);
            await _dataService.FlowStep.CreateAsync(new FlowStep { RootId = created.Data, FlowId = created.Data, FlowStepType = FlowStepTypeEnum.WAIT, Name = "Wait", OrderNumber = 1 }, [], Ct);

            (await _dataService.Flow.DeleteRecordingSetupAsync(created.Data, Ct)).Data.ShouldBeFalse();

            using AppDbContext db = _database.CreateDbContext();
            db.Flows.ShouldHaveSingleItem();
        }
    }
}
