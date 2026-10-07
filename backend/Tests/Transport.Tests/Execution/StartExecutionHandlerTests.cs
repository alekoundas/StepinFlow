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
            return new StartExecutionHandler(_engine, _database, new FlowValidationService()).HandleAsync(new ExecutionStartDto { FlowId = flowId }, Ct);
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

        // Refused before the engine is asked, with the reason the UI shows.
        [Fact]
        public async Task A_flow_with_errors_is_refused_and_says_why()
        {
            int flowId = Seed("Broken", new FlowStep { FlowStepType = FlowStepTypeEnum.CURSOR_CLICK, Name = "Click" });

            ResultDto<int> started = await StartAsync(flowId);

            started.IsSuccess.ShouldBeFalse();
            started.ErrorMessage.ShouldBe("\"Broken\" has errors to fix before it can run. \"Click\": There is no point to act on. Pick a saved point, or a search whose result gives one.");
            _engine.WasStarted.ShouldBeFalse();
        }

        [Fact]
        public async Task A_flow_that_calls_a_sub_flow_with_errors_is_refused_too()
        {
            int brokenId = Seed("Broken", new FlowStep { FlowStepType = FlowStepTypeEnum.CURSOR_CLICK, Name = "Click" });
            int callerId = Seed("Caller", new FlowStep { FlowStepType = FlowStepTypeEnum.SUB_FLOW, Name = "Call it", SubFlowId = brokenId });

            ResultDto<int> started = await StartAsync(callerId);

            started.ErrorMessage.ShouldNotBeNull();
            started.ErrorMessage.ShouldStartWith("\"Broken\" has errors");
            _engine.WasStarted.ShouldBeFalse();
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
