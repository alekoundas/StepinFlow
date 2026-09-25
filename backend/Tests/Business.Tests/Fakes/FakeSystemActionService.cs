using Core.Enums;
using Core.Ports;

namespace Business.Tests.Fakes
{
    public sealed class FakeSystemActionService : ISystemActionService
    {
        public List<SystemActionTypeEnum> Ran { get; } = new List<SystemActionTypeEnum>();

        public void Run(SystemActionTypeEnum action)
        {
            Ran.Add(action);
        }
    }
}
