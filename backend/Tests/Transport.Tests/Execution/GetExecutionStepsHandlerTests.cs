using Core.Enums;
using Core.Models.Database;
using Core.Models.Dtos;
using DataAccess;
using Transport.Ipc.Handlers.Execution;
using Transport.Tests.Fakes;

namespace Transport.Tests.Execution
{
    public sealed class GetExecutionStepsHandlerTests : IDisposable
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

        private Task<ResultDto<List<ExecutionStepDto>>> GetAsync(int executionId, int firstSequence, int lastSequence)
        {
            ExecutionStepRangeDto range = new ExecutionStepRangeDto { ExecutionId = executionId, FirstSequence = firstSequence, LastSequence = lastSequence };
            return new GetExecutionStepsHandler(_database).HandleAsync(range, Ct);
        }

        private int SeedExecution(int stepCount)
        {
            using AppDbContext db = _database.CreateDbContext();

            Flow flow = new Flow { Name = "Looping" };
            db.Flows.Add(flow);
            db.SaveChanges();

            Core.Models.Database.Execution execution = new Core.Models.Database.Execution { FlowId = flow.Id };
            db.Executions.Add(execution);
            db.SaveChanges();

            for (int sequence = stepCount - 1; sequence >= 0; sequence--)
                db.ExecutionSteps.Add(new ExecutionStep { ExecutionId = execution.Id, Sequence = sequence, Name = $"Step {sequence}", FlowStepType = FlowStepTypeEnum.WAIT });

            db.SaveChanges();
            return execution.Id;
        }

        [Fact]
        public async Task The_steps_between_two_sequences_come_back_oldest_first_with_both_ends()
        {
            int executionId = SeedExecution(10);

            ResultDto<List<ExecutionStepDto>> steps = await GetAsync(executionId, 3, 6);

            steps.Data!.Select(x => x.Sequence).ShouldBe([3, 4, 5, 6]);
        }

        [Fact]
        public async Task Another_executions_steps_are_left_out()
        {
            int executionId = SeedExecution(3);
            SeedExecution(3);

            ResultDto<List<ExecutionStepDto>> steps = await GetAsync(executionId, 0, 2);

            steps.Data!.ShouldAllBe(x => x.ExecutionId == executionId);
            steps.Data!.Count.ShouldBe(3);
        }

        // A live execution writes in batches, so the newest steps may not be there yet.
        [Fact]
        public async Task Steps_not_written_yet_are_simply_missing()
        {
            int executionId = SeedExecution(3);

            ResultDto<List<ExecutionStepDto>> steps = await GetAsync(executionId, 1, 50);

            steps.Data!.Select(x => x.Sequence).ShouldBe([1, 2]);
        }

        [Fact]
        public async Task A_score_json_cannot_carry_is_cleared_rather_than_failing_the_reply()
        {
            int executionId = SeedExecution(1);
            using (AppDbContext db = _database.CreateDbContext())
            {
                db.ExecutionSteps.Single().BestScore = float.PositiveInfinity;
                db.SaveChanges();
            }

            ResultDto<List<ExecutionStepDto>> steps = await GetAsync(executionId, 0, 0);

            steps.Data!.Single().BestScore.ShouldBeNull();
        }
    }
}
