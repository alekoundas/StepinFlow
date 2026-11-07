using Core.Enums;
using Core.Models.Database;
using Core.Models.Dtos;
using DataAccess;
using Transport.Ipc.Handlers.Execution;
using Transport.Tests.Fakes;

namespace Transport.Tests.Execution
{
    public sealed class GetExecutionFeedHandlerTests : IDisposable
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

        private async Task<List<ExecutionFeedItemDto>> FeedAsync(ExecutionFeedRequestDto dto)
        {
            ResultDto<List<ExecutionFeedItemDto>> result = await new GetExecutionFeedHandler(_database).HandleAsync(dto, Ct);
            return result.Data!;
        }

        private int SeedFlow(string name, bool isHidden = false)
        {
            using AppDbContext db = _database.CreateDbContext();

            Flow flow = new Flow { Name = name, IsHidden = isHidden };
            db.Flows.Add(flow);
            db.SaveChanges();

            return flow.Id;
        }

        private int SeedExecution(int flowId, ExecutionStatusEnum status, int? errorFlowStepId = null)
        {
            using AppDbContext db = _database.CreateDbContext();

            Core.Models.Database.Execution execution = new Core.Models.Database.Execution { FlowId = flowId, Status = status, ErrorFlowStepId = errorFlowStepId };
            db.Executions.Add(execution);
            db.SaveChanges();

            return execution.Id;
        }

        [Fact]
        public async Task Every_flows_executions_come_newest_first_with_the_flows_name()
        {
            int login = SeedFlow("Login");
            int price = SeedFlow("Price");
            int first = SeedExecution(login, ExecutionStatusEnum.COMPLETED);
            int second = SeedExecution(price, ExecutionStatusEnum.FAILED);

            List<ExecutionFeedItemDto> feed = await FeedAsync(new ExecutionFeedRequestDto());

            feed.Select(x => x.Id).ShouldBe([second, first]);
            feed.Select(x => x.FlowName).ShouldBe(["Price", "Login"]);
        }

        [Fact]
        public async Task A_page_after_another_starts_below_the_id_it_ended_on()
        {
            int flowId = SeedFlow("Login");
            int first = SeedExecution(flowId, ExecutionStatusEnum.COMPLETED);
            int second = SeedExecution(flowId, ExecutionStatusEnum.COMPLETED);
            SeedExecution(flowId, ExecutionStatusEnum.COMPLETED);

            List<ExecutionFeedItemDto> feed = await FeedAsync(new ExecutionFeedRequestDto { BeforeId = second + 1 });

            feed.Select(x => x.Id).ShouldBe([second, first]);
        }

        [Fact]
        public async Task One_flow_and_some_outcomes_narrow_it()
        {
            int login = SeedFlow("Login");
            int price = SeedFlow("Price");
            SeedExecution(login, ExecutionStatusEnum.COMPLETED);
            int failed = SeedExecution(login, ExecutionStatusEnum.FAILED);
            int errored = SeedExecution(login, ExecutionStatusEnum.ERRORED);
            SeedExecution(price, ExecutionStatusEnum.FAILED);

            List<ExecutionFeedItemDto> feed = await FeedAsync(new ExecutionFeedRequestDto
            {
                FlowId = login,
                Statuses = [ExecutionStatusEnum.FAILED, ExecutionStatusEnum.ERRORED],
            });

            feed.Select(x => x.Id).ShouldBe([errored, failed]);
        }

        [Fact]
        public async Task A_flow_the_branch_hides_is_left_out()
        {
            int shown = SeedFlow("Login");
            int hidden = SeedFlow("Gone", true);
            int kept = SeedExecution(shown, ExecutionStatusEnum.COMPLETED);
            SeedExecution(hidden, ExecutionStatusEnum.COMPLETED);

            List<ExecutionFeedItemDto> feed = await FeedAsync(new ExecutionFeedRequestDto());

            feed.Select(x => x.Id).ShouldBe([kept]);
        }

        [Fact]
        public async Task An_errored_execution_names_the_step_that_threw()
        {
            int flowId = SeedFlow("Login");
            int stepId;
            using (AppDbContext db = _database.CreateDbContext())
            {
                FlowStep step = new FlowStep { Name = "Find Login", FlowStepType = FlowStepTypeEnum.SEARCH_IMAGE, FlowId = flowId, RootId = flowId };
                db.FlowSteps.Add(step);
                db.SaveChanges();
                stepId = step.Id;
            }
            SeedExecution(flowId, ExecutionStatusEnum.ERRORED, stepId);
            SeedExecution(flowId, ExecutionStatusEnum.COMPLETED);

            List<ExecutionFeedItemDto> feed = await FeedAsync(new ExecutionFeedRequestDto());

            feed.Select(x => x.ErrorStepName).ShouldBe([null, "Find Login"]);
        }

        // Without a zone it goes out without one, and the page reads it as local time.
        [Fact]
        public async Task Its_times_are_marked_as_utc()
        {
            SeedExecution(SeedFlow("Login"), ExecutionStatusEnum.COMPLETED);
            using (AppDbContext db = _database.CreateDbContext())
            {
                db.Executions.Single().CompletedAt = new DateTime(2026, 11, 7, 12, 0, 0);
                db.SaveChanges();
            }

            ExecutionFeedItemDto item = (await FeedAsync(new ExecutionFeedRequestDto())).Single();

            item.CreatedOn.Kind.ShouldBe(DateTimeKind.Utc);
            item.CompletedAt!.Value.Kind.ShouldBe(DateTimeKind.Utc);
        }
    }
}
