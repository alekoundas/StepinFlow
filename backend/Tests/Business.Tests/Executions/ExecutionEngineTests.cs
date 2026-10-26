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
    /// The engine end to end on SQLite: what it broadcasts, the Execution row it leaves, how it ends,
    /// and how the debugger holds it. Every step passes and does nothing unless a test says what a
    /// step of that name does.
    /// </summary>
    public sealed class ExecutionEngineTests : IDisposable
    {
        private static readonly TimeSpan TIMEOUT = TimeSpan.FromSeconds(10);

        private readonly TestDatabase _database = new TestDatabase();
        private readonly FakeIpcBroadcastService _broadcast = new FakeIpcBroadcastService();
        private readonly ScriptedWorkers _workers = new ScriptedWorkers();

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

            return new ExecutionEngine(_database, _workers, cache, history, _broadcast, new FakeInputSimulatorService(), TimeProvider.System, NullLogger<ExecutionEngine>.Instance);
        }

        // Steps in script order; a step with a ParentFlowStep sits under it.
        private int SeedFlow(bool isQaTest, params FlowStep[] steps)
        {
            using AppDbContext db = _database.CreateDbContext();

            Flow flow = new Flow { Name = "Login", PublicId = Guid.NewGuid(), IsQaTest = isQaTest };
            db.Flows.Add(flow);
            db.SaveChanges();

            for (int i = 0; i < steps.Length; i++)
            {
                FlowStep step = steps[i];
                step.RootId = flow.Id;
                step.OrderNumber = steps.Take(i).Count(x => x.ParentFlowStep == step.ParentFlowStep);

                if (step.ParentFlowStep == null)
                    step.FlowId = flow.Id;

                db.FlowSteps.Add(step);
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

        private async Task<ExecutionEngine> HeldAtAsync(int flowId, params FlowStep[] breakpoints)
        {
            ExecutionEngine engine = Engine();
            await engine.StartAsync(new ExecutionStartDto { FlowId = flowId, Breakpoints = [.. breakpoints.Select(x => x.Id)] }, Ct);
            await _broadcast.SentAsync(PausedAt(breakpoints[0])).WaitAsync(TIMEOUT, Ct);

            return engine;
        }

        private static Func<object?, bool> PausedAt(FlowStep step)
        {
            return x => x is ExecutionEventDto e && e.Type == ExecutionEventTypeEnum.PAUSED && e.FlowStepId == step.Id;
        }

        private List<ExecutionEventDto> Events()
        {
            lock (_broadcast.Sent)
            {
                return _broadcast.Sent
                    .Select(x => x.Payload)
                    .OfType<ExecutionEventDto>()
                    .ToList();
            }
        }

        private ExecutionEventDto? Ended()
        {
            return Events().FirstOrDefault(x => x.Type == ExecutionEventTypeEnum.RUN_ENDED);
        }

        private List<string> Started()
        {
            return Events()
                .Where(x => x.Type == ExecutionEventTypeEnum.STEP_STARTED)
                .Select(x => x.Name)
                .ToList();
        }

        private List<Execution> ExecutionRows()
        {
            using AppDbContext db = _database.CreateDbContext();
            return db.Executions.ToList();
        }

        private static FlowStep Wait(string name = "Wait")
        {
            return new FlowStep { Name = name, FlowStepType = FlowStepTypeEnum.WAIT };
        }

        private static FlowStep Under(FlowStep parent, FlowStep child)
        {
            child.ParentFlowStep = parent;
            return child;
        }


        // ================================================================
        // How it ends
        // ================================================================

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
        public async Task Each_step_is_broadcast_as_it_starts_and_finishes_then_the_end_and_the_row_agrees()
        {
            int flowId = SeedFlow(false, Wait("First"), Wait("Second"));

            await ExecuteAsync(flowId);

            Events().Select(x => (x.Type, x.Name)).ShouldBe(
            [
                (ExecutionEventTypeEnum.STEP_STARTED, "First"),
                (ExecutionEventTypeEnum.STEP_FINISHED, "First"),
                (ExecutionEventTypeEnum.STEP_STARTED, "Second"),
                (ExecutionEventTypeEnum.STEP_FINISHED, "Second"),
                (ExecutionEventTypeEnum.RUN_ENDED, string.Empty),
            ]);

            Execution row = ExecutionRows().ShouldHaveSingleItem();
            row.Status.ShouldBe(ExecutionStatusEnum.COMPLETED);
            row.StepCount.ShouldBe(2);
            row.CompletedAt.ShouldNotBeNull();
            row.ErrorFlowStepId.ShouldBeNull();
        }

        [Fact]
        public async Task A_worker_that_throws_errors_the_execution_on_its_step()
        {
            FlowStep broken = Wait("Broken");
            int flowId = SeedFlow(false, Wait("First"), broken, Wait("Never"));
            _workers.Does("Broken", ct => throw new InvalidOperationException("The screen went away."));

            (await ExecuteAsync(flowId)).ShouldBe(ExecutionStatusEnum.ERRORED);

            Started().ShouldBe(["First", "Broken"]);
            Execution row = ExecutionRows().ShouldHaveSingleItem();
            row.Status.ShouldBe(ExecutionStatusEnum.ERRORED);
            row.ErrorFlowStepId.ShouldBe(broken.Id);
            row.ErrorMessage.ShouldBe("The screen went away.");
        }

        [Fact]
        public async Task Stop_during_a_step_ends_the_execution_as_stopped_not_errored()
        {
            int flowId = SeedFlow(false, Wait("Slow"), Wait("Never"));
            TaskCompletionSource started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            _workers.Does("Slow", async ct =>
            {
                started.TrySetResult();
                await Task.Delay(Timeout.InfiniteTimeSpan, ct);
                return ExecutionStep.Success();
            });

            ExecutionEngine engine = Engine();
            await engine.StartAsync(new ExecutionStartDto { FlowId = flowId }, Ct);
            await started.Task.WaitAsync(TIMEOUT, Ct);

            engine.Stop();
            await engine.Completion.WaitAsync(TIMEOUT, Ct);

            Started().ShouldBe(["Slow"]);
            Execution row = ExecutionRows().ShouldHaveSingleItem();
            row.Status.ShouldBe(ExecutionStatusEnum.STOPPED);
            row.ErrorFlowStepId.ShouldBeNull();
            engine.IsRunning.ShouldBeFalse();
        }


        // ================================================================
        // One at a time
        // ================================================================

        [Fact]
        public async Task A_second_start_is_refused_while_one_is_going()
        {
            FlowStep first = Wait("First");
            int flowId = SeedFlow(false, first);
            ExecutionEngine engine = await HeldAtAsync(flowId, first);

            await Should.ThrowAsync<InvalidOperationException>(() => engine.StartAsync(new ExecutionStartDto { FlowId = flowId }, Ct));

            engine.Stop();
            await engine.Completion.WaitAsync(TIMEOUT, Ct);

            ExecutionRows().ShouldHaveSingleItem().Status.ShouldBe(ExecutionStatusEnum.STOPPED);
        }

        [Fact]
        public async Task A_start_that_fails_before_the_walk_leaves_the_engine_free()
        {
            int flowId = SeedFlow(false, Wait());
            ExecutionEngine engine = Engine();
            using CancellationTokenSource cancelled = new CancellationTokenSource();
            await cancelled.CancelAsync();

            Exception? thrown = await Record.ExceptionAsync(() => engine.StartAsync(new ExecutionStartDto { FlowId = flowId }, cancelled.Token));

            thrown.ShouldBeAssignableTo<OperationCanceledException>();
            engine.IsRunning.ShouldBeFalse();

            await engine.StartAsync(new ExecutionStartDto { FlowId = flowId }, Ct);
            await engine.Completion.WaitAsync(TIMEOUT, Ct);

            Ended()!.Status.ShouldBe(ExecutionStatusEnum.COMPLETED);
        }


        // ================================================================
        // The debugger
        // ================================================================

        [Fact]
        public async Task A_breakpoint_holds_the_walk_until_Continue()
        {
            FlowStep second = Wait("Second");
            int flowId = SeedFlow(false, Wait("First"), second);

            ExecutionEngine engine = await HeldAtAsync(flowId, second);

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

            ExecutionEngine engine = await HeldAtAsync(flowId, first);

            engine.StepInto();
            await _broadcast.SentAsync(PausedAt(second)).WaitAsync(TIMEOUT, Ct);

            Started().ShouldBe(["First"]);

            engine.Continue();
            await engine.Completion.WaitAsync(TIMEOUT, Ct);

            Started().ShouldBe(["First", "Second", "Third"]);
        }

        [Fact]
        public async Task Step_Over_executes_what_is_under_the_step_and_holds_after_it()
        {
            FlowStep loop = new FlowStep { Name = "Loop", FlowStepType = FlowStepTypeEnum.LOOP, LoopCount = 1 };
            FlowStep after = Wait("After");
            int flowId = SeedFlow(false, loop, Under(loop, Wait("Inside 1")), Under(loop, Wait("Inside 2")), after);

            ExecutionEngine engine = await HeldAtAsync(flowId, loop);

            engine.StepOver();
            await _broadcast.SentAsync(PausedAt(after)).WaitAsync(TIMEOUT, Ct);

            Started().ShouldBe(["Loop", "Inside 1", "Inside 2"]);

            engine.Continue();
            await engine.Completion.WaitAsync(TIMEOUT, Ct);
        }

        // What every debugger does: a breakpoint wins over the step being stepped past.
        [Fact]
        public async Task A_breakpoint_inside_a_stepped_over_subtree_still_holds_there()
        {
            FlowStep loop = new FlowStep { Name = "Loop", FlowStepType = FlowStepTypeEnum.LOOP, LoopCount = 1 };
            FlowStep inside = Under(loop, Wait("Inside 2"));
            int flowId = SeedFlow(false, loop, Under(loop, Wait("Inside 1")), inside, Wait("After"));

            ExecutionEngine engine = await HeldAtAsync(flowId, loop, inside);

            engine.StepOver();
            await _broadcast.SentAsync(PausedAt(inside)).WaitAsync(TIMEOUT, Ct);

            Started().ShouldBe(["Loop", "Inside 1"]);

            engine.Continue();
            await engine.Completion.WaitAsync(TIMEOUT, Ct);

            Started().ShouldBe(["Loop", "Inside 1", "Inside 2", "After"]);
        }

        [Fact]
        public async Task Stop_while_held_ends_the_execution_as_stopped()
        {
            FlowStep first = Wait("First");
            int flowId = SeedFlow(false, first);

            ExecutionEngine engine = await HeldAtAsync(flowId, first);

            engine.Stop();
            await engine.Completion.WaitAsync(TIMEOUT, Ct);

            Started().ShouldBeEmpty();
            Ended()!.Status.ShouldBe(ExecutionStatusEnum.STOPPED);
        }


        // Every step passes and does nothing, unless a test says what a step of that name does.
        private sealed class ScriptedWorkers : IStepWorkerFactory, IStepWorker
        {
            private readonly Dictionary<string, Func<CancellationToken, Task<ExecutionStep>>> _byName = new Dictionary<string, Func<CancellationToken, Task<ExecutionStep>>>();

            public void Does(string stepName, Func<CancellationToken, Task<ExecutionStep>> work)
            {
                _byName[stepName] = work;
            }

            public IStepWorker GetWorker(FlowStepTypeEnum flowStepType)
            {
                return this;
            }

            public Task<ExecutionStep> ExecuteAsync(FlowStep step, IExecutionCacheService cache, CancellationToken ct)
            {
                if (_byName.TryGetValue(step.Name, out Func<CancellationToken, Task<ExecutionStep>>? work))
                    return work(ct);

                return Task.FromResult(ExecutionStep.Success());
            }
        }
    }
}
