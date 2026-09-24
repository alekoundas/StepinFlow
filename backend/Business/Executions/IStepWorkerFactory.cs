using Business.Executions.Workers;
using Core.Enums;

namespace Business.Executions
{
    public interface IStepWorkerFactory
    {
        IStepWorker GetWorker(FlowStepTypeEnum flowStepType);
    }
}
