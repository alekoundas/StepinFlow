using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Business.Notification;

using App.DependencyInjection;
using App.AutoMapper;

using Transport.Ipc;
using Transport.Ipc.Handlers;
using Transport.Ipc.Handlers.Ai;
using Transport.Ipc.Handlers.Execution;
using Transport.Ipc.Handlers.Lookup;

using Business.Shell;
using Business.Executions;
using Business.Searching;
using Business.AreaPoint;
using Business.Flows.DataService;
using Business.Flows.DataService.DataHandlers;
using Business.FlowScript;
using Business.FlowScript.Scanner;
using Business.FlowScript.Text;
using Business.Flows.FlowValidationService;
using Business.Ai;
using Business.AppSettings;
using Business.Recording.Draft;
using Business.Recording.Elements;
using Business.Recording.Generation;
using Business.Recording.Screens;
using Business.Recording.Session;
using Business.Recording.Setup;
using Business.Ai.Providers;
using Business.Ai.Helpers;
using Business.Ai.AiDocuments;
using Business.Ai.AiModels;
using Business.Ai.Vision;
using Business.Ai.ScriptRevision;
using Business.Repository;
using Business.ScreenText;

using DataAccess;

using Core.Catalogs;
using Core.Helpers;
using Core.Ports;

using Platform.Shared.Git;
using Platform.Shared.Input;
using Platform.Shared.Ocr;
using Platform.Shared.Vision;

using Platform.Windows.Input;
using Platform.Windows.Native;
using Platform.Windows.Accessibility;
using Platform.Windows.Screen;
using Platform.Windows.SystemActions;
using Platform.Windows.Windowing;

namespace App
{
    internal sealed class Program
    {
        public static async Task Main(string[] args)
        {
            // First thing, before any coordinate API: without it Windows virtualizes every rect
            // to 96 DPI and nothing matches the capture buffers or the input hook.
            ScreenMetrics.EnablePerMonitorDpiAwareness();

            HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);

            // Logging. Electron writes the backend's output to main.log, which rotates at 1 MB, and EF
            // Core's every SQL command at Information filled it in under two minutes.
            builder.Logging.ClearProviders();
            builder.Logging.AddConsole();
            builder.Logging.AddFilter("Microsoft.EntityFrameworkCore", LogLevel.Warning);


            // DB context factory. 
            AppDbContextInitialize.AddCustomDbContextFactory(builder.Services);


            // TimeProvider offers an abstraction over the DateTime.UtcNow. That means the methods are easy to unit test.
            builder.Services.AddSingleton(TimeProvider.System);

            // Ports
            builder.Services.AddSingleton<IOpenCvService, OpenCvService>();
            builder.Services.AddSingleton<IMouseService, MouseService>();
            builder.Services.AddSingleton<IInputSimulatorService, InputSimulatorService>();
            builder.Services.AddSingleton<IInputRecordService, InputRecordService>();
            builder.Services.AddSingleton<IScreenshotService, ScreenshotService>();
            builder.Services.AddSingleton<IScreenService, ScreenService>();
            builder.Services.AddSingleton<IWindowService, WindowService>();
            builder.Services.AddSingleton<ISystemActionService, SystemActionService>();
            builder.Services.AddSingleton<IProcessService, ProcessService>();
            // The folders a service works in are given here, so a test can give folders of its own.
            builder.Services.AddSingleton<IOcrService>(x => new PaddleOcrService(PathHelper.GetAiModelsPath(), PathHelper.GetOcrReadersPath()));
            builder.Services.AddSingleton<IAccessibleElementService, AccessibleElementService>();
            builder.Services.AddSingleton<IBrowserTabService, BrowserTabService>();
            builder.Services.AddSingleton<IWindowsGraphicsCaptureService, WindowsGraphicsCaptureService>();

            // Services
            builder.Services.AddSingleton<BrowserTabFinder>();
            builder.Services.AddSingleton<IAreaPointResolver, AreaPointResolver>();
            builder.Services.AddSingleton<IImageSearcher, ImageSearcher>();
            builder.Services.AddSingleton<IShellCommandExecutor, ShellCommandExecutor>();
            builder.Services.AddSingleton<IAppSettingService, AppSettingService>();
            builder.Services.AddSingleton<IRecordingSessionService, RecordingSessionService>();
            builder.Services.AddSingleton<IClickedElementFinder, ClickedElementFinder>();
            builder.Services.AddSingleton<IClickedElementGrouper, ClickedElementGrouper>();
            builder.Services.AddSingleton<IRecordedScreenReader, RecordedScreenReader>();
            builder.Services.AddSingleton<IRecordingScreenDescriber, RecordingScreenDescriber>();
            builder.Services.AddSingleton<IRecordingDraftBuilder, RecordingDraftBuilder>();
            builder.Services.AddSingleton<IRecordingWordsService, RecordingWordsService>();
            builder.Services.AddSingleton<IFlowGenerationService, FlowGenerationService>();
            builder.Services.AddSingleton<IFlowGenerationSessionService, FlowGenerationSessionService>();
            builder.Services.AddSingleton<IElementLocator, ElementLocator>();
            builder.Services.AddSingleton<IRecordingSetupService, RecordingSetupService>();

            // Validation
            builder.Services.AddSingleton<IFlowValidationService, FlowValidationService>();

            // Data services
            builder.Services.AddSingleton<FlowDataService>();
            builder.Services.AddSingleton<FlowStepDataService>();
            builder.Services.AddSingleton<FlowAreaDataService>();
            builder.Services.AddSingleton<FlowPointDataService>();
            builder.Services.AddSingleton<FlowInputDataService>();
            builder.Services.AddSingleton<FlowTemplateDataService>();
            builder.Services.AddSingleton<DiscordBotDataService>();
            builder.Services.AddSingleton<ExecutionDataService>();
            builder.Services.AddSingleton<DataService>();

            // Flow script
            builder.Services.AddSingleton<IPrinter, Printer>();
            builder.Services.AddSingleton<IScanner, Scanner>();
            builder.Services.AddSingleton<IFlowScriptExporter, FlowScriptExporter>();
            builder.Services.AddSingleton<IFlowScriptImporter, FlowScriptImporter>();
            builder.Services.AddSingleton<IFlowScriptPreviewService, FlowScriptPreviewService>();
            builder.Services.AddSingleton<IFlowStepScriptLineService, FlowStepScriptLineService>();

            // Repository.
            builder.Services.AddSingleton<IGitService, GitService>();
            builder.Services.AddSingleton<IGitRepositoryService>(x => ActivatorUtilities.CreateInstance<GitRepositoryService>(x, PathHelper.GetRepositoryPath()));
            builder.Services.AddSingleton<IFlowRestoreService, FlowRestoreService>();
            builder.Services.AddSingleton<IBranchSwitchService, BranchSwitchService>();

            ExecutionServiceRegistration.AddExecutionEngine(builder.Services);

            // AI.
            // Singletons: they hold nothing between calls, and the recording's generation and the chat's answers, both singletons, ask them.
            // The client is rebuilt from settings each time, so changing provider or key in Settings takes effect on the next question.
            builder.Services.AddSingleton<IOllamaService, OllamaService>();
            builder.Services.AddSingleton<IAiProviderService, AiProviderService>();
            builder.Services.AddSingleton<IAiClientFactory, AiClientFactory>();
            builder.Services.AddSingleton<IScriptRevisionService, ScriptRevisionService>();
            builder.Services.AddSingleton<IAiModelService, AiModelService>();
            builder.Services.AddScoped<IExecutionRunExplainService, ExecutionRunExplainService>();
            builder.Services.AddSingleton<IFlowQuestionService, FlowQuestionService>();
            builder.Services.AddSingleton<IExecutionScreenshotReader, ExecutionScreenshotReader>();
            builder.Services.AddSingleton<IAiChatSessionService, AiChatSessionService>();
            builder.Services.AddSingleton<IAiModelDownloadService, AiModelDownloadService>();

            // Singletons, unlike the rest of AI: these hold the loaded onnx model and the built index, which cost seconds to produce and nothing to keep.
            builder.Services.AddSingleton<IEmbeddingService, OnnxEmbeddingService>();
            builder.Services.AddSingleton<IAiDocumentIndexService, AiDocumentIndexService>();
            builder.Services.AddSingleton<IFlowExampleIndex, FlowExampleIndex>();

            // Ollama is on this machine, so a short timeout is right.
            builder.Services.AddHttpClient(nameof(OllamaService), client =>
            {
                client.Timeout = TimeSpan.FromSeconds(5);
            }).ConfigurePrimaryHttpMessageHandler(OllamaConnectionHelper.CreateHandler);

            // Infinite for downloading 9 gigabytes.
            builder.Services.AddHttpClient(nameof(AiModelDownloadService), client =>
            {
                client.Timeout = Timeout.InfiniteTimeSpan;
            }).ConfigurePrimaryHttpMessageHandler(OllamaConnectionHelper.CreateHandler);

            // What the model answers through. HttpClient's own 100 seconds, as when OllamaSharp made its own client.
            builder.Services.AddHttpClient(nameof(AiClientFactory)).ConfigurePrimaryHttpMessageHandler(OllamaConnectionHelper.CreateHandler);

            // A text reader is eight megabytes or so.
            builder.Services.AddHttpClient(nameof(OcrReaderDownloadService), client =>
            {
                client.Timeout = TimeSpan.FromMinutes(5);
            });
            builder.Services.AddSingleton<IOcrReaderDownloadService>(x => ActivatorUtilities.CreateInstance<OcrReaderDownloadService>(x, PathHelper.GetOcrReadersPath(), OcrReaderCatalog.All));

            // Notifications.
            // The queue is a singleton because the throttle is per bot and has to be remembered between flows, not per request.
            builder.Services.AddHttpClient(nameof(DiscordNotifier), client =>
            {
                client.Timeout = TimeSpan.FromSeconds(15);
            });
            builder.Services.AddSingleton<IDiscordNotifier, DiscordNotifier>();
            builder.Services.AddSingleton<IDiscordSendQueue, DiscordSendQueue>();


            // IPC
            builder.Services.AddSingleton<IpcRequestPipe>();
            builder.Services.AddSingleton<IpcBroadcastPipe>();
            builder.Services.AddSingleton<IpcDispatcher>();
            builder.Services.AddSingleton<IIpcBroadcastService, IpcBroadcastService>();

            // IPC handlers.
            builder.Services.AddTransient<CreateFlowHandler>();
            builder.Services.AddTransient<UpdateFlowHandler>();
            builder.Services.AddTransient<DeleteFlowHandler>();
            builder.Services.AddTransient<GetFlowHandler>();
            builder.Services.AddTransient<GetLazyFlowHandler>();
            builder.Services.AddTransient<ValidateFlowHandler>();
            builder.Services.AddTransient<GetFlowHealthHandler>();
            builder.Services.AddTransient<GetFlowCallersHandler>();
            builder.Services.AddTransient<PromoteFlowToSubFlowHandler>();
            builder.Services.AddTransient<SearchFlowsHandler>();
            builder.Services.AddTransient<ExtractSubFlowHandler>();
            builder.Services.AddTransient<GetFlowTreeNodeHandler>();
            builder.Services.AddTransient<GetFlowStepUsesHandler>();
            builder.Services.AddTransient<ExportFlowScriptHandler>();
            builder.Services.AddTransient<ImportFlowScriptHandler>();
            builder.Services.AddTransient<PreviewFlowScriptHandler>();
            builder.Services.AddTransient<GetFlowScriptExportFolderHandler>();
            builder.Services.AddTransient<RenderFlowScriptHandler>();
            builder.Services.AddTransient<GetRepositoryStatusHandler>();
            builder.Services.AddTransient<GetRepositoryChangesHandler>();
            builder.Services.AddTransient<GetFlowRepositoryChangesHandler>();
            builder.Services.AddTransient<CommitRepositoryHandler>();
            builder.Services.AddTransient<GetFlowRepositoryHistoryHandler>();
            builder.Services.AddTransient<GetFlowRepositoryCommitHandler>();
            builder.Services.AddTransient<DiscardFlowChangesHandler>();
            builder.Services.AddTransient<RestoreFlowVersionHandler>();
            builder.Services.AddTransient<GetRepositoryBranchesHandler>();
            builder.Services.AddTransient<CreateRepositoryBranchHandler>();
            builder.Services.AddTransient<SwitchRepositoryBranchHandler>();
            builder.Services.AddTransient<ConnectRepositoryRemoteHandler>();
            builder.Services.AddTransient<PreviewRepositoryPullHandler>();
            builder.Services.AddTransient<PullRepositoryHandler>();
            builder.Services.AddTransient<PushRepositoryHandler>();
            builder.Services.AddTransient<CreateDiscordBotHandler>();
            builder.Services.AddTransient<UpdateDiscordBotHandler>();
            builder.Services.AddTransient<DeleteDiscordBotHandler>();
            builder.Services.AddTransient<GetDiscordBotHandler>();
            builder.Services.AddTransient<GetLazyDiscordBotHandler>();
            builder.Services.AddTransient<TestDiscordBotHandler>();
            builder.Services.AddTransient<CreateFlowStepHandler>();
            builder.Services.AddTransient<UpdateFlowStepHandler>();
            builder.Services.AddTransient<SetFlowStepDisabledHandler>();
            builder.Services.AddTransient<DeleteFlowStepHandler>();
            builder.Services.AddTransient<GetFlowStepHandler>();
            builder.Services.AddTransient<GetLazyFlowStepHandler>();
            builder.Services.AddTransient<GetFlowStepTreeNodeHandler>();
            builder.Services.AddTransient<GetFlowStepTreeNodesRecursiveHandler>();
            builder.Services.AddTransient<GetFlowStepDeleteImpactHandler>();
            builder.Services.AddTransient<GetFlowStepMovePreviewHandler>();
            builder.Services.AddTransient<MoveFlowStepHandler>();
            builder.Services.AddTransient<PasteFlowStepHandler>();
            builder.Services.AddTransient<GetFlowStepScriptLineHandler>();
            builder.Services.AddTransient<TestImageSearchHandler>();
            builder.Services.AddTransient<TestShellCommandHandler>();
            builder.Services.AddTransient<TestCursorStepHandler>();
            builder.Services.AddTransient<TestSearchTextHandler>();
            builder.Services.AddTransient<CreateFlowAreaHandler>();
            builder.Services.AddTransient<UpdateFlowAreaHandler>();
            builder.Services.AddTransient<DeleteFlowAreaHandler>();
            builder.Services.AddTransient<GetFlowAreaHandler>();
            builder.Services.AddTransient<GetLazyFlowAreaHandler>();
            builder.Services.AddTransient<GetFlowAreaPreviewHandler>();
            builder.Services.AddTransient<CreateFlowPointHandler>();
            builder.Services.AddTransient<UpdateFlowPointHandler>();
            builder.Services.AddTransient<DeleteFlowPointHandler>();
            builder.Services.AddTransient<GetFlowPointHandler>();
            builder.Services.AddTransient<GetFlowPointPreviewHandler>();
            builder.Services.AddTransient<ResolveFlowPointHandler>();
            builder.Services.AddTransient<CreateFlowInputHandler>();
            builder.Services.AddTransient<UpdateFlowInputHandler>();
            builder.Services.AddTransient<DeleteFlowInputHandler>();
            builder.Services.AddTransient<GetFlowTemplatesHandler>();
            builder.Services.AddTransient<UpdateFlowTemplateHandler>();
            builder.Services.AddTransient<GetLookupWindowHandler>();
            builder.Services.AddTransient<GetLookupMonitorHandler>();
            builder.Services.AddTransient<GetLookupFlowStepHandler>();
            builder.Services.AddTransient<GetLookupFlowPointHandler>();
            builder.Services.AddTransient<GetLookupSubFlowHandler>();
            builder.Services.AddTransient<GetLookupDiscordBotHandler>();
            builder.Services.AddTransient<GetLookupReportedStepHandler>();
            builder.Services.AddTransient<GetLookupBreakHandler>();
            builder.Services.AddTransient<TestWindowMatchHandler>();
            builder.Services.AddTransient<GetLookupLaunchCommandHandler>();
            builder.Services.AddTransient<GetLookupFlowAreaHandler>();
            builder.Services.AddTransient<GetLookupOcrLanguagesHandler>();
            builder.Services.AddTransient<GetLookupAiModelsHandler>();
            builder.Services.AddTransient<GetLookupAiModelSuggestionsHandler>();
            builder.Services.AddTransient<SetupRecordingHandler>();
            builder.Services.AddTransient<StartRecordingHandler>();
            builder.Services.AddTransient<PauseRecordingHandler>();
            builder.Services.AddTransient<ResumeRecordingHandler>();
            builder.Services.AddTransient<StopRecordingHandler>();
            builder.Services.AddTransient<DiscardRecordingHandler>();
            builder.Services.AddTransient<GetRecordingResultHandler>();
            builder.Services.AddTransient<GetRecordingScreenHandler>();
            builder.Services.AddTransient<ReviewRecordingHandler>();
            builder.Services.AddTransient<GetRecordingGestureHandler>();
            builder.Services.AddTransient<DecideRecordingGestureHandler>();
            builder.Services.AddTransient<TestRecordingGestureRuleHandler>();
            builder.Services.AddTransient<ImportRecordingDraftHandler>();
            builder.Services.AddTransient<InsertRecordingDraftHandler>();
            builder.Services.AddTransient<GenerateRecordingDraftHandler>();
            builder.Services.AddTransient<ReviseRecordingDraftHandler>();
            builder.Services.AddTransient<DropProposedCheckHandler>();
            builder.Services.AddTransient<MarkRecordingStageHandler>();
            builder.Services.AddTransient<GetGeneratedRecordingDraftHandler>();
            builder.Services.AddTransient<SendRecordingDraftRequestHandler>();
            builder.Services.AddTransient<CancelRecordingDraftGenerationHandler>();
            builder.Services.AddTransient<GetAppSettingsHandler>();
            builder.Services.AddTransient<SetAppSettingHandler>();
            builder.Services.AddTransient<SystemTakeScreenshotHandler>();
            builder.Services.AddTransient<SystemCaptureForOverlayHandler>();
            builder.Services.AddTransient<SystemCropScreenshotsHandler>();
            builder.Services.AddTransient<SystemMoveCursorHandler>();
            builder.Services.AddTransient<SystemDownloadOcrLanguageHandler>();
            builder.Services.AddTransient<SystemInputRecordAllStartHandler>();
            builder.Services.AddTransient<SystemInputRecordAllStopHandler>();
            builder.Services.AddTransient<SystemInputRecordOverlayStartHandler>();
            builder.Services.AddTransient<SystemInputRecordOverlayStopHandler>();
            builder.Services.AddTransient<SystemInputRecordPointCaptureStartHandler>();
            builder.Services.AddTransient<SystemInputRecordPointCaptureStopHandler>();
            builder.Services.AddTransient<SystemInputRecordHotkeyStartHandler>();
            builder.Services.AddTransient<SystemInputRecordHotkeyStopHandler>();
            builder.Services.AddTransient<StartExecutionHandler>();
            builder.Services.AddTransient<StopExecutionHandler>();
            builder.Services.AddTransient<PauseExecutionHandler>();
            builder.Services.AddTransient<ContinueExecutionHandler>();
            builder.Services.AddTransient<StepIntoExecutionHandler>();
            builder.Services.AddTransient<StepOverExecutionHandler>();
            builder.Services.AddTransient<SetExecutionBreakpointsHandler>();
            builder.Services.AddTransient<GetExecutionHandler>();
            builder.Services.AddTransient<GetExecutionStepsHandler>();
            builder.Services.AddTransient<GetExecutionListHandler>();
            builder.Services.AddTransient<GetExecutionStateHandler>();
            builder.Services.AddTransient<GetFlowExecutionSummariesHandler>();
            builder.Services.AddTransient<GetExecutionFeedHandler>();
            builder.Services.AddTransient<GetExecutionStatsHandler>();
            builder.Services.AddTransient<GetExecutionStepScreenshotHandler>();
            builder.Services.AddTransient<DeleteExecutionHandler>();
            builder.Services.AddTransient<DeleteFlowExecutionsHandler>();
            builder.Services.AddTransient<ExplainExecutionHandler>();
            builder.Services.AddTransient<GetAiStatusHandler>();
            builder.Services.AddTransient<GetAiLocalModelStatusHandler>();
            builder.Services.AddTransient<FreeAiLocalModelHandler>();
            builder.Services.AddTransient<GetAiChatAvailabilityHandler>();
            builder.Services.AddTransient<GetAiChatImagesHandler>();
            builder.Services.AddTransient<AskAiHandler>();
            builder.Services.AddTransient<StopAiAnswerHandler>();
            builder.Services.AddTransient<DownloadAiModelHandler>();
            builder.Services.AddTransient<GetAiDownloadStateHandler>();
            builder.Services.AddTransient<ClearAiDownloadStateHandler>();


            // AutoMapper
            builder.Services.AddAutoMapper(config => config.AddProfile<AutoMapperProfile>());


            // Localization (JSON)
            //builder.Services.AddSingleton<IStringLocalizerFactory, JsonLocalizerFactory>();
            //builder.Services.AddTransient(typeof(IStringLocalizer), typeof(JsonLocalizer));


            // On launch: the database first, then the rest side by side.
            builder.Services.AddHostedService(x => ActivatorUtilities.CreateInstance<HostedLifecycleService>(x, Console.OpenStandardInput())); // The database before anything starts; stops when Electron closes stdin, the engine first.
            builder.Services.AddHostedService<HostedRequestPipeListener>();    // Requests from Electron.
            builder.Services.AddHostedService<HostedBroadcastPipeListener>();  // Events to Electron.
            builder.Services.AddHostedService<HostedGitRepositoryService>();   // The repository folder, kept a copy of the flows.
            builder.Services.AddHostedService<HostedAiDocumentIndexService>(); // The help docs, embedded once.
            builder.Services.AddHostedService<HostedSharpHookService>();       // The global input hook: hotkeys and recording.
            builder.Services.AddHostedService(x => ActivatorUtilities.CreateInstance<HostedShutdownService>(x, Console.OpenStandardInput())); // Stops when Electron closes stdin, the engine first.


            IHost app = builder.Build();
            await app.RunAsync();
        }
    }



    // Main Pipe
    internal sealed class HostedRequestPipeListener : BackgroundService
    {
        private readonly IpcRequestPipe _ipcRequestPipe;
        public HostedRequestPipeListener(IpcRequestPipe ipcRequestPipe) => _ipcRequestPipe = ipcRequestPipe;
        protected override Task ExecuteAsync(CancellationToken cancellationToken)
        {
            return _ipcRequestPipe.StartBackgroundService(cancellationToken);
        }
    }

    // Broadcast Pipe
    internal sealed class HostedBroadcastPipeListener : BackgroundService
    {
        private readonly IpcBroadcastPipe _ipcBroadcastPipe;
        public HostedBroadcastPipeListener(IpcBroadcastPipe ipcBroadcastPipe) => _ipcBroadcastPipe = ipcBroadcastPipe;
        protected override Task ExecuteAsync(CancellationToken cancellationToken)
        {
            return _ipcBroadcastPipe.StartBackgroundService(cancellationToken);
        }
    }


    // Sharphook global input recording hook.
    internal sealed class HostedSharpHookService : BackgroundService
    {
        private readonly IInputRecordService _inputRecordService;
        public HostedSharpHookService(IInputRecordService inputRecordService) => _inputRecordService = inputRecordService;
        protected override Task ExecuteAsync(CancellationToken cancellationToken)
        {
            return _inputRecordService.StartGlobalHookAsync(cancellationToken);
        }
    }

    // Embed the docs once.
    internal sealed class HostedAiDocumentIndexService : BackgroundService
    {
        private readonly IAiDocumentIndexService _aiDocumentIndexService;
        private readonly ILogger<HostedAiDocumentIndexService> _logger;

        public HostedAiDocumentIndexService(IAiDocumentIndexService aiDocumentIndexService, ILogger<HostedAiDocumentIndexService> logger)
        {
            _aiDocumentIndexService = aiDocumentIndexService;
            _logger = logger;
        }

        protected override Task ExecuteAsync(CancellationToken cancellationToken)
        {
            return Task.Run(() =>
            {
                try
                {
                    if (_aiDocumentIndexService.IsAvailable())
                        _logger.LogInformation("Ai document index is ready.");
                    else
                        _logger.LogWarning("Ai document index is unavailable, so help questions are answered without the docs. The embedding model in AiModels is missing or unreadable.");
                }
                catch (Exception exception)
                {
                    // A background service that throws stops the host by default, and an index for
                    // the help docs is not worth the app failing to start over.
                    _logger.LogError(exception, "Building the ai document index failed.");
                }
            }, cancellationToken);
        }
    }

    // Git repository.
    // On new thread: making the repository and writing every flow is file work the host should not wait for.
    internal sealed class HostedGitRepositoryService : BackgroundService
    {
        private readonly IGitRepositoryService _gitRepositoryService;

        public HostedGitRepositoryService(IGitRepositoryService gitRepositoryService)
        {
            _gitRepositoryService = gitRepositoryService;
        }

        protected override Task ExecuteAsync(CancellationToken cancellationToken)
        {
            return Task.Run(() => _gitRepositoryService.StartBackgroundService(cancellationToken), cancellationToken);
        }
    }




    // The app's start and stop.
    // The host waits for every StartingAsync before it starts any hosted service, and for every StoppingAsync before it stops one.
    // Start: the database. Stop: when the stdin closes, gracefully everything.
    internal sealed class HostedLifecycleService : BackgroundService, IHostedLifecycleService
    {
        private readonly IDbContextFactory<AppDbContext> _dbContextFactory;
        private readonly TimeProvider _timeProvider;
        private readonly ILogger<HostedLifecycleService> _logger;
        private readonly IHostApplicationLifetime _lifetime;
        private readonly IExecutionEngine _executionEngine;
        private readonly Stream _stdin;

        public HostedLifecycleService(
            IDbContextFactory<AppDbContext> dbContextFactory,
            TimeProvider timeProvider,
            ILogger<HostedLifecycleService> logger,
            IHostApplicationLifetime lifetime,
            IExecutionEngine executionEngine,
            Stream stdin)
        {
            _dbContextFactory = dbContextFactory;
            _timeProvider = timeProvider;
            _logger = logger;
            _lifetime = lifetime;
            _executionEngine = executionEngine;
            _stdin = stdin;
        }

        public async Task StartingAsync(CancellationToken cancellationToken)
        {
            // Run migrations and seed data.
            await using AppDbContext dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
            dbContext.Database.Migrate();
            AppDbContextInitialize.SetWALMode(dbContext);

            if (!AppDbContextInitialize.SetVacuumMode(dbContext))
                _logger.LogWarning("The database could not switch on incremental vacuum, so deleting keeps the file at its size.");

            await AppDbContextInitialize.AbandonUnfinishedExecutionsAsync(dbContext, _timeProvider);
        }

        protected override async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            // Keep reading stdin until it closes (ex. Electron closes it)
            await _stdin.CopyToAsync(Stream.Null, CancellationToken.None).WaitAsync(cancellationToken);

            // Gently stop the WHOLE .NET!
            // 1) StoppingAsync below stops the execution still going.
            // 2) Stops hosted services one at a time, last registered goes first.
            // 3) Disposes the services in Main().
            _lifetime.StopApplication();
        }

        // Before any hosted service stops, so the execution writes its history while the pipes and the database are still there.
        public async Task StoppingAsync(CancellationToken cancellationToken)
        {
            if (_executionEngine.IsRunning)
                _executionEngine.Stop();

            await _executionEngine.Completion.WaitAsync(cancellationToken);
        }

        public Task StartedAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public Task StoppedAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
