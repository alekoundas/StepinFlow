using Core.Enums;

namespace Core.Ports
{
    // Dependency Inversion Principle(DIP)
    public interface ISystemActionService
    {
        void Run(SystemActionTypeEnum action);
    }
}
