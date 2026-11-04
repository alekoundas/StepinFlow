using Business.Flows.FlowValidationService;
using Core.Enums;
using Core.Models.Database;
using Core.Models.Dtos;
using DataAccess;
using Transport.Ipc.Handlers;
using Transport.Tests.Fakes;

namespace Transport.Tests.Execution
{
    public sealed class StartExecutionHandlerTests : IDisposable
    {
        private readonly TestDatabase _database = new TestDatabase();
        private readonly FakeExecutionEngine _engine = new FakeExecutionEngine();
        private readonly FakeDebuggerHotkeyService _hotkeys = new FakeDebuggerHotkeyService();

        public void Dispose()
        {
            _database.Dispose();
        }

        private static CancellationToken Ct
        {
            get { return TestContext.Current.CancellationToken; }
        }

        private Task<ResultDto<int>> StartAsync(int flowId)
        {
            return new StartExecutionHandler(_engine, _database, new FlowValidationService(), _hotkeys).HandleAsync(new ExecutionStartDto { FlowId = flowId }, Ct);
        }

        private int Seed(string name, FlowStep step)
        {
            using AppDbContext db = _database.CreateDbContext();

            Flow flow = new Flow { Name = name };
            db.Flows.Add(flow);
            db.SaveChanges();

            step.RootId = flow.Id;
            step.FlowId = flow.Id;
            db.FlowSteps.Add(step);
            db.SaveChanges();

            return flow.Id;
        }

        [Fact]
        public async Task A_flow_with_no_errors_is_started()
        {
            int flowId = Seed("Fine", new FlowStep { FlowStepType = FlowStepTypeEnum.WAIT, Name = "Wait", WaitForMilliseconds = 100 });

            ResultDto<int> started = await StartAsync(flowId);

            started.IsSuccess.ShouldBeTrue();
            _engine.FlowId.ShouldBe(flowId);
        }

        // Settings may have changed them since the last execution, and nothing listens between two.
        [Fact]
        public async Task The_debugger_keys_are_read_and_listened_for_as_an_execution_starts()
        {
            int flowId = Seed("Fine", new FlowStep { FlowStepType = FlowStepTypeEnum.WAIT, Name = "Wait", WaitForMilliseconds = 100 });

            await StartAsync(flowId);

            (_hotkeys.BindCount, _hotkeys.WasListening).ShouldBe((1, true));
        }

        // Refused before the engine is asked, with the reason the UI shows.
        [Fact]
        public async Task A_flow_with_errors_is_refused_and_says_why()
        {
            int flowId = Seed("Broken", new FlowStep { FlowStepType = FlowStepTypeEnum.CURSOR_RELOCATE, Name = "Move" });

            ResultDto<int> started = await StartAsync(flowId);

            started.IsSuccess.ShouldBeFalse();
            started.ErrorMessage.ShouldBe("\"Broken\" has errors to fix before it can run. \"Move\": There is no point to act on. Pick a saved point, or a search whose result gives one.");
            _engine.WasStarted.ShouldBeFalse();
        }

        [Fact]
        public async Task A_flow_that_calls_a_sub_flow_with_errors_is_refused_too()
        {
            int brokenId = Seed("Broken", new FlowStep { FlowStepType = FlowStepTypeEnum.CURSOR_RELOCATE, Name = "Move" });
            int callerId = Seed("Caller", new FlowStep { FlowStepType = FlowStepTypeEnum.SUB_FLOW, Name = "Call it", SubFlowId = brokenId });

            ResultDto<int> started = await StartAsync(callerId);

            started.ErrorMessage.ShouldNotBeNull();
            started.ErrorMessage.ShouldStartWith("\"Broken\" has errors");
            _engine.WasStarted.ShouldBeFalse();
        }

        // A half-built step can be parked, and the rest tried.
        [Fact]
        public async Task An_error_in_a_disabled_step_does_not_stop_the_flow()
        {
            int flowId = Seed("Parked", new FlowStep { FlowStepType = FlowStepTypeEnum.CURSOR_RELOCATE, Name = "Move", IsDisabled = true });

            ResultDto<int> started = await StartAsync(flowId);

            started.IsSuccess.ShouldBeTrue();
        }

        [Fact]
        public async Task A_sub_flow_with_errors_called_by_a_disabled_step_does_not_stop_the_flow()
        {
            int brokenId = Seed("Broken", new FlowStep { FlowStepType = FlowStepTypeEnum.CURSOR_RELOCATE, Name = "Move" });
            int callerId = Seed("Caller", new FlowStep { FlowStepType = FlowStepTypeEnum.SUB_FLOW, Name = "Call it", SubFlowId = brokenId, IsDisabled = true });

            ResultDto<int> started = await StartAsync(callerId);

            started.IsSuccess.ShouldBeTrue();
        }

        // Named only by the call, since it has no step of its own to be loaded by.
        [Fact]
        public async Task A_flow_that_calls_an_empty_sub_flow_is_refused()
        {
            int emptyId;
            using (AppDbContext db = _database.CreateDbContext())
            {
                Flow empty = new Flow { Name = "Empty" };
                db.Flows.Add(empty);
                db.SaveChanges();
                emptyId = empty.Id;
            }

            int callerId = Seed("Caller", new FlowStep { FlowStepType = FlowStepTypeEnum.SUB_FLOW, Name = "Call it", SubFlowId = emptyId });

            ResultDto<int> started = await StartAsync(callerId);

            started.ErrorMessage.ShouldBe("\"Empty\" has errors to fix before it can run. This flow has no steps yet.");
            _engine.WasStarted.ShouldBeFalse();
        }
    }
}
