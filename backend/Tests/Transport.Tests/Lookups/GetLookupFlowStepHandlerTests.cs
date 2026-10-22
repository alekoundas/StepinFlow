using Core.Enums;
using Core.Models.Database;
using Core.Models.Dtos;
using DataAccess;
using Transport.Ipc.Handlers;
using Transport.Tests.Fakes;

namespace Transport.Tests.Lookups
{
    /// <summary>The steps a Move, a Drag or a Check Value is offered to read.</summary>
    public sealed class GetLookupFlowStepHandlerTests : IDisposable
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
        public async Task A_search_that_waits_until_it_finds_nothing_is_not_offered()
        {
            using AppDbContext db = _database.CreateDbContext();

            Flow flow = new Flow { Name = "Invoices" };
            db.Flows.Add(flow);
            db.SaveChanges();

            FlowStep loaded = Step(flow, FlowStepTypeEnum.SEARCH_IMAGE, "Page loaded", null);
            loaded.SearchMode = SearchModeEnum.WAIT_UNTIL_FOUND;
            Save(db, loaded);

            FlowStep loadedSuccess = Save(db, Step(flow, FlowStepTypeEnum.SUCCESS, string.Empty, loaded));

            FlowStep spinnerGone = Step(flow, FlowStepTypeEnum.SEARCH_IMAGE, "Spinner gone", loadedSuccess);
            spinnerGone.SearchMode = SearchModeEnum.WAIT_UNTIL_NOT_FOUND;
            Save(db, spinnerGone);

            FlowStep goneSuccess = Save(db, Step(flow, FlowStepTypeEnum.SUCCESS, string.Empty, spinnerGone));
            FlowStep move = Save(db, Step(flow, FlowStepTypeEnum.CURSOR_RELOCATE, "Move", goneSuccess));

            LookupRequestDto request = new LookupRequestDto { FlowStepId = move.Id, ResultKind = StepResultKindEnum.LOCATION };
            ResultDto<LookupResponseDto> result = await new GetLookupFlowStepHandler(_database).HandleAsync(request, Ct);
            LookupResponseDto response = result.Data!;

            response.Data.Select(x => x.Label).ShouldBe(["Page loaded"]);
        }

        private static FlowStep Step(Flow flow, FlowStepTypeEnum type, string name, FlowStep? parent)
        {
            return new FlowStep { RootId = flow.Id, FlowId = flow.Id, FlowStepType = type, Name = name, ParentFlowStepId = parent?.Id };
        }

        private static FlowStep Save(AppDbContext db, FlowStep step)
        {
            db.FlowSteps.Add(step);
            db.SaveChanges();

            return step;
        }
    }
}
