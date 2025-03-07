using Business.Factories.Workers;
using Business.Services.Interfaces;
using Model.Enums;

namespace Business.Factories
{
    public class ExecutionFactory : IExecutionFactory
    {
        private readonly IDataService _dataService;
        private readonly ISystemService _systemService;
        private readonly ITemplateSearchService _templateSearchService;
        private Dictionary<FlowStepTypesEnum, Lazy<IExecutionWorker>>? _workerCache = null;

        private CancellationTokenSource _cancellationToken = new CancellationTokenSource();


        public ExecutionFactory(IDataService dataService, ISystemService systemService, ITemplateSearchService templateSearchService)
        {
            _dataService = dataService;
            _systemService = systemService;
            _templateSearchService = templateSearchService;
        }

        public IExecutionWorker GetWorker(FlowStepTypesEnum? Type)
        {
            if (_workerCache == null)
                _workerCache = GetWorkers();

            // Lazy initialization (only created on the first access).
            if (Type.HasValue && _workerCache.TryGetValue(Type.Value, out var lazyWorker))
                return lazyWorker.Value;

            // Default worker for null or unrecognized types.
            // should never get here
            return new FlowExecutionWorker(_dataService, _systemService);
        }

        public void SetCancellationToken(CancellationTokenSource cancellationToken)
        {
            _cancellationToken = cancellationToken;
        }

        public void DestroyWorkers()
        {
            _workerCache = null;
        }

        private Dictionary<FlowStepTypesEnum, Lazy<IExecutionWorker>> GetWorkers()
        {
            return new Dictionary<FlowStepTypesEnum, Lazy<IExecutionWorker>>()
            {
                { FlowStepTypesEnum.WINDOW_MOVE, new Lazy<IExecutionWorker>(() => new WindowMoveExecutionWorker(_dataService, _systemService)) },
                { FlowStepTypesEnum.WINDOW_RESIZE, new Lazy<IExecutionWorker>(() => new WindowResizeExecutionWorker(_dataService, _systemService)) },
                { FlowStepTypesEnum.CURSOR_RELOCATE, new Lazy<IExecutionWorker>(() => new CursorRelocateExecutionWorker(_dataService, _systemService)) },
                { FlowStepTypesEnum.CURSOR_CLICK, new Lazy<IExecutionWorker>(() => new CursorClickExecutionWorker(_dataService, _systemService)) },
                { FlowStepTypesEnum.CURSOR_SCROLL, new Lazy<IExecutionWorker>(() => new CursorScrollExecutionWorker(_dataService, _systemService)) },
                { FlowStepTypesEnum.WAIT_FOR_TEMPLATE, new Lazy<IExecutionWorker>(() => new WaitForTemplateExecutionWorker(_dataService, _systemService, _templateSearchService)) },
                { FlowStepTypesEnum.TEMPLATE_SEARCH, new Lazy<IExecutionWorker>(() => new TemplateSearchExecutionWorker(_dataService, _systemService, _templateSearchService)) },
                { FlowStepTypesEnum.MULTIPLE_TEMPLATE_SEARCH, new Lazy<IExecutionWorker>(() => new MultipleTemplateSearchExecutionWorker(_dataService, _systemService, _templateSearchService)) },
                { FlowStepTypesEnum.WAIT, new Lazy<IExecutionWorker>(() => new WaitExecutionWorker(_dataService, _systemService,_cancellationToken)) },
                { FlowStepTypesEnum.GO_TO, new Lazy<IExecutionWorker>(() => new GoToExecutionWorker(_dataService, _systemService)) },
                { FlowStepTypesEnum.LOOP, new Lazy<IExecutionWorker>(() => new LoopExecutionWorker(_dataService, _systemService)) },
                { FlowStepTypesEnum.SUB_FLOW_STEP, new Lazy<IExecutionWorker>(() => new SubFlowStepExecutionWorker(_dataService, _systemService)) }
            };
        }

    }
}
