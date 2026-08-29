using Business.Services.ExecutionService.Workers;
using Core.Enums;

namespace Business.Services.ExecutionService
{
    public class StepWorkerFactory : IStepWorkerFactory
    {
        private readonly IReadOnlyDictionary<FlowStepTypeEnum, IStepWorker> _workersByType;
        private readonly IStepWorker _passThrough;

        public StepWorkerFactory(IReadOnlyDictionary<FlowStepTypeEnum, IStepWorker> workersByType, IStepWorker passThrough)
        {
            _workersByType = workersByType;
            _passThrough = passThrough;
        }


        // ================================================================
        // Public methods
        // ================================================================

        public IStepWorker GetWorker(FlowStepTypeEnum flowStepType)
        {
            // Get instance by type.
            if (!_workersByType.TryGetValue(flowStepType, out IStepWorker? worker))
                return _passThrough;// Success, Failure, Loop, Go To and Sub-Flow 

            return worker;
        }
    }
}
