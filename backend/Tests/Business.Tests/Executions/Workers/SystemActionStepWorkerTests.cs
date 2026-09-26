using Business.Executions.Workers;
using Business.Tests.Fakes;
using Core.Enums;
using Core.Models.Database;
using static Business.Tests.TestToken;

namespace Business.Tests.Executions.Workers
{
    /// <summary>
    /// Twenty-one lines that hand an action to the machine and report whether it went. There is one
    /// decision in it and this is it; everything else about locking a workstation belongs to the
    /// adapter, which is why there is only one test here.
    /// </summary>
    public sealed class SystemActionStepWorkerTests
    {
        [Fact]
        public async Task A_system_action_is_handed_to_the_machine()
        {
            FakeSystemActionService system = new FakeSystemActionService();
            FlowStep step = new FlowStep { Id = 1, FlowStepType = FlowStepTypeEnum.SYSTEM_ACTION, SystemActionType = SystemActionTypeEnum.LOCK_WORKSTATION };

            await new SystemActionStepWorker(system).ExecuteAsync(step, await WorkerCache.ForAsync(step), Ct);

            system.Ran.ShouldBe([SystemActionTypeEnum.LOCK_WORKSTATION]);
        }
    }
}
