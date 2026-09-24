using Business.Services.ExecutionService.Workers;
using Core.Enums;

namespace Business.Services.ExecutionService
{
    public interface IStepWorkerFactory
    {
        IStepWorker GetWorker(FlowStepTypeEnum flowStepType);
    }
}
