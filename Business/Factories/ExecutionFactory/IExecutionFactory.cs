using Model.Enums;

namespace Business.Factories.ExecutionFactory
{
    public interface IExecutionFactory
    {
        IExecutionWorker GetWorker(FlowStepTypesEnum? flowStep);
        void SetCancellationToken(CancellationTokenSource cancellationToken);
        void DestroyWorkers();
    }
}
