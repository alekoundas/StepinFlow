using Core.Enums;
using Core.Models.Database;
using Core.Models.Dtos;
using DataAccess;
using Transport.Ipc.Handlers;
using Transport.Tests.Fakes;

namespace Transport.Tests.Flows
{
    /// <summary>
    /// What the editor's tabs read to say which steps use an area, a location, an input or a
    /// template.
    /// </summary>
    public sealed class GetFlowStepUsesHandlerTests : IDisposable
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

        [Fact]
        public async Task A_step_says_which_area_inputs_and_templates_it_uses()
        {
            using AppDbContext db = _database.CreateDbContext();

            Flow flow = new Flow { Name = "Invoices" };
            db.Flows.Add(flow);
            db.SaveChanges();

            FlowArea area = new FlowArea { FlowId = flow.Id, Name = "Billing portal", Type = FlowAreaTypeEnum.MONITOR };
            db.FlowAreas.Add(area);
            db.SaveChanges();

            FlowStep search = new FlowStep { RootId = flow.Id, FlowId = flow.Id, FlowStepType = FlowStepTypeEnum.SEARCH_IMAGE, Name = "Email field", FlowAreaId = area.Id };
            FlowStep type = new FlowStep { RootId = flow.Id, FlowId = flow.Id, FlowStepType = FlowStepTypeEnum.KEYBOARD_INPUT, Name = "Type", KeyboardInputText = "{{email}} and {{EMAIL}}" };
            db.FlowSteps.AddRange(search, type);
            db.SaveChanges();

            db.FlowStepTemplates.Add(new FlowStepTemplate { FlowStepId = search.Id, Name = "template-e4m2q.png" });
            db.SaveChanges();

            ResultDto<List<FlowStepUseDto>> result = await new GetFlowStepUsesHandler(_database).HandleAsync(flow.Id, Ct);
            List<FlowStepUseDto> uses = result.Data!;

            FlowStepUseDto searchUse = uses.Single(x => x.FlowStepId == search.Id);
            searchUse.FlowAreaId.ShouldBe(area.Id);
            searchUse.TemplateNames.ShouldBe(["template-e4m2q.png"]);

            FlowStepUseDto typeUse = uses.Single(x => x.FlowStepId == type.Id);
            typeUse.VariableNames.ShouldBe(["email"]);
            typeUse.TemplateNames.ShouldBeEmpty();
        }
    }
}
