using Business.Flows.FlowValidationService;
using Core.Enums;
using Core.Models.Database;
using Core.Models.Dtos;
using DataAccess;
using Transport.Ipc.Handlers;
using Transport.Tests.Fakes;

namespace Transport.Tests.Flows
{
    /// <summary>
    /// The counts behind the Flows list's badges and Home's Needs attention.
    /// Test:
    /// 1) Each flow comes with its name, and whether it is a sub-flow, so Home can list it on its own
    /// 2) A flow hidden by the branch is left out
    /// 3) A flow's issues are counted, and a disabled step's are not
    /// 4) No flows at all is an empty answer
    /// </summary>
    public sealed class GetFlowHealthHandlerTests : IDisposable
    {
        private readonly TestDatabase _database = new TestDatabase();

        public void Dispose()
        {
            _database.Dispose();
        }

        private static CancellationToken Ct
        {
            get { return TestContext.Current.CancellationToken; }
        }

        private async Task<IReadOnlyList<FlowHealthDto>> HealthAsync()
        {
            GetFlowHealthHandler handler = new GetFlowHealthHandler(_database, new FlowValidationService(new FakeOcrService()));
            ResultDto<IReadOnlyList<FlowHealthDto>> result = await handler.HandleAsync(new FlowHealthRequestDto(), Ct);

            return result.Data!;
        }

        [Fact]
        public async Task A_flow_comes_with_its_name_and_whether_it_is_a_sub_flow()
        {
            using AppDbContext db = _database.CreateDbContext();
            db.Flows.Add(new Flow { Name = "Invoices" });
            db.Flows.Add(new Flow { Name = "Log in", IsSubFlow = true });
            db.SaveChanges();

            IReadOnlyList<FlowHealthDto> health = await HealthAsync();

            health.Single(x => x.FlowName == "Invoices").IsSubFlow.ShouldBeFalse();
            health.Single(x => x.FlowName == "Log in").IsSubFlow.ShouldBeTrue();
        }

        [Fact]
        public async Task A_flow_the_branch_hid_is_left_out()
        {
            using AppDbContext db = _database.CreateDbContext();
            db.Flows.Add(new Flow { Name = "Invoices" });
            db.Flows.Add(new Flow { Name = "Gone", IsHidden = true });
            db.SaveChanges();

            IReadOnlyList<FlowHealthDto> health = await HealthAsync();

            health.Select(x => x.FlowName).ShouldBe(["Invoices"]);
        }

        [Fact]
        public async Task A_flows_issues_are_counted_and_a_disabled_steps_are_not()
        {
            using AppDbContext db = _database.CreateDbContext();

            Flow empty = new Flow { Name = "Empty" };
            Flow broken = new Flow { Name = "Broken" };
            Flow parked = new Flow { Name = "Parked" };
            db.Flows.AddRange(empty, broken, parked);
            db.SaveChanges();

            // A Move with nowhere to go is an error, until it is disabled.
            db.FlowSteps.Add(new FlowStep { RootId = broken.Id, FlowId = broken.Id, FlowStepType = FlowStepTypeEnum.CURSOR_RELOCATE, Name = "Move" });
            db.FlowSteps.Add(new FlowStep { RootId = parked.Id, FlowId = parked.Id, FlowStepType = FlowStepTypeEnum.CURSOR_RELOCATE, Name = "Move", IsDisabled = true });
            db.FlowSteps.Add(new FlowStep { RootId = parked.Id, FlowId = parked.Id, FlowStepType = FlowStepTypeEnum.WAIT, Name = "Pause", WaitForMilliseconds = 100 });
            db.SaveChanges();

            IReadOnlyList<FlowHealthDto> health = await HealthAsync();

            health.Single(x => x.FlowId == empty.Id).ErrorCount.ShouldBe(1);
            health.Single(x => x.FlowId == broken.Id).ErrorCount.ShouldBeGreaterThan(0);

            FlowHealthDto parkedHealth = health.Single(x => x.FlowId == parked.Id);
            parkedHealth.ErrorCount.ShouldBe(0);
            parkedHealth.WarningCount.ShouldBe(0);
        }

        [Fact]
        public async Task No_flows_is_an_empty_answer()
        {
            (await HealthAsync()).ShouldBeEmpty();
        }
    }
}
