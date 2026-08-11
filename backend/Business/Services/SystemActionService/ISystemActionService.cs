using Core.Enums;

namespace Business.Services.SystemActionService
{
    public interface ISystemActionService
    {
        void Run(SystemActionTypeEnum action);
    }
}
