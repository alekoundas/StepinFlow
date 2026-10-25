using Business.Executions;
using Business.Executions.Workers;
using Business.FlowScript;
using Business.FlowScript.Text;
using Business.Tests.Fakes;
using Core.Enums;
using Core.Models.Database;
using Core.Models.Dtos;
using DataAccess;
using Microsoft.Extensions.Logging.Abstractions;
using static Business.Tests.TestToken;

namespace Business.Tests.Executions
{
    /// <summary>
    /// How an execution ends when the walk reaches the end. Every step passes and does nothing, so
    /// the verdict comes from the flow alone.
    /// </summary>
    public sealed class ExecutionEngineTests : IDisposable
    {
        private readonly TestDatabase _database = new TestDatabase();
        private readonly FakeIpcBroadcastService _broadcast = new FakeIpcBroadcastService();

        public void Dispose()
        {
            _database.Dispose();
        }

        private ExecutionEngine Engine()
        {
            FakeAppSettingService settings = new FakeAppSettingService();
            ExecutionCacheService cache = new ExecutionCacheService(settings, new FakeScreenshotService(), TimeProvider.System);
            FlowScriptExporter exporter = new FlowScriptExporter(_database, new Printer());
            ExecutionHistoryService history = new ExecutionHistoryService(_database, cache, settings, new FakeGitRepositoryService(), exporter, TimeProvider.System, NullLogger<ExecutionHistoryService>.Instance);

            return new ExecutionEngine(_database, new PassThroughWorkers(), cache, history, _broadcast, new FakeInputSimulatorService(), TimeProvider.System, NullLogger<ExecutionEngine>.Instance);
        }

        private int SeedFlow(bool isQaTest, params FlowStep[] steps)
        {
            using AppDbContext db = _database.CreateDbContext();

            Flow flow = new Flow { Name = "Login", PublicId = Guid.NewGuid(), IsQaTest = isQaTest };
            db.Flows.Add(flow);
            db.SaveChanges();

            for (int i = 0; i < steps.Length; i++)
            {
                steps[i].FlowId = flow.Id;
                steps[i].RootId = flow.Id;
                steps[i].OrderNumber = i;
                db.FlowSteps.Add(steps[i]);
            }

            db.SaveChanges();
            return flow.Id;
        }

        private async Task<ExecutionStatusEnum?> ExecuteAsync(int flowId)
        {
            await Engine().StartAsync(new ExecutionStartDto { FlowId = flowId }, Ct);

            using CancellationTokenSource timeout = CancellationTokenSource.CreateLinkedTokenSource(Ct);
            timeout.CancelAfter(TimeSpan.FromSeconds(10));

            while (Ended() == null)
                await Task.Delay(10, timeout.Token);

            return Ended()!.Status;
        }

        private ExecutionEventDto? Ended()
        {
            lock (_broadcast.Sent)
            {
                return _broadcast.Sent
                    .Select(x => x.Payload)
                    .OfType<ExecutionEventDto>()
                    .FirstOrDefault(x => x.Type == ExecutionEventTypeEnum.RUN_ENDED);
            }
        }

        private static FlowStep Wait()
        {
            return new FlowStep { Name = "Wait", FlowStepType = FlowStepTypeEnum.WAIT };
        }


        [Fact]
        public async Task An_automation_that_reaches_its_end_completed()
        {
            int flowId = SeedFlow(false, Wait());

            (await ExecuteAsync(flowId)).ShouldBe(ExecutionStatusEnum.COMPLETED);
        }

        [Fact]
        public async Task A_qa_test_that_reaches_its_end_without_saying_is_inconclusive()
        {
            int flowId = SeedFlow(true, Wait());

            (await ExecuteAsync(flowId)).ShouldBe(ExecutionStatusEnum.INCONCLUSIVE);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task An_End_Execution_decides_the_verdict_of_either_kind(bool isQaTest)
        {
            FlowStep end = new FlowStep { Name = "End", FlowStepType = FlowStepTypeEnum.END_EXECUTION, EndExecutionAsSuccess = false, Message = "did not sign in" };
            int flowId = SeedFlow(isQaTest, Wait(), end);

            (await ExecuteAsync(flowId)).ShouldBe(ExecutionStatusEnum.FAILED);
        }


        // Every step passes and does nothing.
        private sealed class PassThroughWorkers : IStepWorkerFactory
        {
            public IStepWorker GetWorker(FlowStepTypeEnum flowStepType)
            {
                return new PassThroughStepWorker();
            }
        }
    }
}
