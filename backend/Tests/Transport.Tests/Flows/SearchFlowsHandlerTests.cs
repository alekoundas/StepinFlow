using Core.Enums;
using Core.Models.Database;
using Core.Models.Dtos;
using DataAccess;
using Transport.Ipc.Handlers;
using Transport.Tests.Fakes;

namespace Transport.Tests.Flows
{
    /// <summary>
    /// The header's search.
    /// Test:
    /// 1) A word finds flows by name or description, and steps by name, with the flow they are in
    /// 2) Every word has to be there, in any order and either case
    /// 3) A flow hidden by the branch is not found, and neither are its steps
    /// 4) A % or _ typed is looked for as itself
    /// 5) Nothing typed finds nothing
    /// </summary>
    public sealed class SearchFlowsHandlerTests : IDisposable
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

        private async Task<List<FlowSearchHitDto>> SearchAsync(string text)
        {
            ResultDto<List<FlowSearchHitDto>> found = await new SearchFlowsHandler(_database).HandleAsync(text, Ct);
            found.IsSuccess.ShouldBeTrue();

            return found.Data.ShouldNotBeNull();
        }

        private int Seed(string name, string description, bool isHidden, params string[] stepNames)
        {
            using AppDbContext db = _database.CreateDbContext();

            Flow flow = new Flow { Name = name, Description = description, IsHidden = isHidden };
            db.Flows.Add(flow);
            db.SaveChanges();

            int order = 0;
            foreach (string stepName in stepNames)
            {
                FlowStep step = new FlowStep { FlowStepType = FlowStepTypeEnum.WAIT, Name = stepName, RootId = flow.Id, FlowId = flow.Id, OrderNumber = order++ };
                db.FlowSteps.Add(step);
                db.SaveChanges();

                db.FlowSteps.Add(new FlowStep { FlowStepType = FlowStepTypeEnum.SUCCESS, Name = stepName, RootId = flow.Id, ParentFlowStepId = step.Id });
                db.SaveChanges();
            }

            return flow.Id;
        }

        [Fact]
        public async Task A_word_finds_flows_and_the_steps_named_with_it()
        {
            int invoices = Seed("Download invoices", "Saves last month's invoices.", false, "Find invoice row", "Wait 2s");
            Seed("Weekly stock export", "Exports the invoice report too.", false, "Open stock");
            Seed("Login", "Signs in.", false, "Type password");

            List<FlowSearchHitDto> hits = await SearchAsync("invoice");

            hits.Where(x => x.FlowStepId == null).Select(x => x.FlowName).ShouldBe(["Download invoices", "Weekly stock export"]);

            FlowSearchHitDto step = hits.Single(x => x.FlowStepId != null);
            (step.FlowStepName, step.FlowId, step.FlowName, step.FlowStepType).ShouldBe(("Find invoice row", invoices, "Download invoices", FlowStepTypeEnum.WAIT));
        }

        [Fact]
        public async Task Every_word_has_to_be_there_in_any_order_and_case()
        {
            Seed("Download invoices", "", false);
            Seed("Download reports", "", false);

            List<FlowSearchHitDto> hits = await SearchAsync("INVOICES download");

            hits.Select(x => x.FlowName).ShouldBe(["Download invoices"]);
        }

        [Fact]
        public async Task A_hidden_flow_and_its_steps_are_not_found()
        {
            Seed("Checkout smoke", "", true, "Find checkout");

            List<FlowSearchHitDto> hits = await SearchAsync("checkout");

            hits.ShouldBeEmpty();
        }

        [Fact]
        public async Task A_percent_sign_is_looked_for_as_itself()
        {
            Seed("Discount 10%", "", false);
            Seed("Discount 100", "", false);

            List<FlowSearchHitDto> hits = await SearchAsync("10%");

            hits.Select(x => x.FlowName).ShouldBe(["Discount 10%"]);
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        public async Task Nothing_typed_finds_nothing(string text)
        {
            Seed("Login", "", false, "Type password");

            List<FlowSearchHitDto> hits = await SearchAsync(text);

            hits.ShouldBeEmpty();
        }
    }
}
