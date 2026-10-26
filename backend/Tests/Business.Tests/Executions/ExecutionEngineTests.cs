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
    /// How an execution ends, and how the debugger holds it. Every step passes and does nothing, so
    /// the verdict comes from the flow alone.
    /// </summary>
    public sealed class ExecutionEngineTests : IDisposable
    {
        private static readonly TimeSpan TIMEOUT = TimeSpan.FromSeconds(10);

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
            ExecutionEngine engine = Engine();
            await engine.StartAsync(new ExecutionStartDto { FlowId = flowId }, Ct);
            await engine.Completion.WaitAsync(TIMEOUT, Ct);

            return Ended()!.Status;
        }

        private async Task<ExecutionEngine> PausedAtAsync(int flowId, FlowStep breakpoint)
        {
            ExecutionEngine engine = Engine();
            await engine.StartAsync(new ExecutionStartDto { FlowId = flowId, Breakpoints = [breakpoint.Id] }, Ct);
            await _broadcast.SentAsync(PausedAt(breakpoint)).WaitAsync(TIMEOUT, Ct);

            return engine;
        }

        private static Func<object?, bool> PausedAt(FlowStep step)
        {
            return x => x is ExecutionEventDto e && e.Type == ExecutionEventTypeEnum.PAUSED && e.FlowStepId == step.Id;
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

        private List<string> Started()
        {
            lock (_broadcast.Sent)
            {
                return _broadcast.Sent
                    .Select(x => x.Payload)
                    .OfType<ExecutionEventDto>()
                    .Where(x => x.Type == ExecutionEventTypeEnum.STEP_STARTED)
                    .Select(x => x.Name)
                    .ToList();
            }
        }

        private static FlowStep Wait(string name = "Wait")
        {
            return new FlowStep { Name = name, FlowStepType = FlowStepTypeEnum.WAIT };
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

        [Fact]
        public async Task A_breakpoint_holds_the_walk_until_Continue()
        {
            FlowStep second = Wait("Second");
            int flowId = SeedFlow(false, Wait("First"), second);

            ExecutionEngine engine = await PausedAtAsync(flowId, second);

            Started().ShouldBe(["First"]);
            engine.Completion.IsCompleted.ShouldBeFalse();

            engine.Continue();
            await engine.Completion.WaitAsync(TIMEOUT, Ct);

            Started().ShouldBe(["First", "Second"]);
            Ended()!.Status.ShouldBe(ExecutionStatusEnum.COMPLETED);
        }

        [Fact]
        public async Task Step_Into_executes_one_step_and_holds_at_the_next()
        {
            FlowStep first = Wait("First");
            FlowStep second = Wait("Second");
            int flowId = SeedFlow(false, first, second, Wait("Third"));

            ExecutionEngine engine = await PausedAtAsync(flowId, first);

            engine.StepInto();
            await _broadcast.SentAsync(PausedAt(second)).WaitAsync(TIMEOUT, Ct);

            Started().ShouldBe(["First"]);

            engine.Continue();
            await engine.Completion.WaitAsync(TIMEOUT, Ct);

            Started().ShouldBe(["First", "Second", "Third"]);
        }

        [Fact]
        public async Task Stop_while_held_ends_the_execution_as_stopped()
        {
            FlowStep first = Wait("First");
            int flowId = SeedFlow(false, first);

            ExecutionEngine engine = await PausedAtAsync(flowId, first);

            engine.Stop();
            await engine.Completion.WaitAsync(TIMEOUT, Ct);

            Started().ShouldBeEmpty();
            Ended()!.Status.ShouldBe(ExecutionStatusEnum.STOPPED);
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
