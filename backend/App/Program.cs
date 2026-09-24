using App.AutoMapper;
using Transport.Ipc;
using Transport.Ipc.Handlers;
using Transport.Ipc.Handlers.Ai;
using Transport.Ipc.Handlers.Execution;
using Transport.Ipc.Handlers.Lookup;
using Business.Command;
using Business.Searching;
using Business.AreaPoint;
using Business.FlowScript;
using Business.FlowScript.Syntax;
using Business.FlowScript.Text;
using Business.Validation;
using Core.Ports;
using Business.Ai;
using Business.AppSettings;
using Business.Recording;
using DataAccess;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Business.Notification;
using App.DependencyInjection;
using Core.Enums;
using Business.Ai.Providers;
using Business.Ai.AiDocuments;
using Business.Ai.AiModels;
using Platform.Windows.Input;
using Platform.Windows.Native;
using Platform.Windows.Ocr;
using Platform.Windows.Screen;
using Platform.Windows.SystemActions;
using Platform.Windows.Vision;
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

            // Logging
            builder.Logging.ClearProviders();
            builder.Logging.AddConsole();


            // DB context factory. Handlers own their DbContext and their own write statements.
            builder.Services.AddCustomDbContextFactory();


            // TimeProvider offers an abstraction over the DateTime.UtcNow. That means the methods are easy to unit test.
            builder.Services.AddSingleton(TimeProvider.System);

            // Ports
            builder.Services.AddSingleton<IOpenCvService, OpenCvService>();
            builder.Services.AddSingleton<IInputService, InputService>();
            builder.Services.AddSingleton<IInputRecordService, InputRecordService>();
            builder.Services.AddSingleton<IScreenshotService, ScreenshotService>();
            builder.Services.AddSingleton<IScreenService, ScreenService>();
            builder.Services.AddSingleton<IWindowService, WindowService>();
            builder.Services.AddSingleton<ISystemActionService, SystemActionService>();
            builder.Services.AddSingleton<IProcessService, ProcessService>();
            builder.Services.AddSingleton<IOcrService, OcrService>();
            builder.Services.AddSingleton<IWindowsGraphicsCaptureService, WindowsGraphicsCaptureService>();

            // Services
            builder.Services.AddSingleton<IAreaPointResolver, AreaPointResolver>();
            builder.Services.AddSingleton<IImageSearcher, ImageSearcher>();
            builder.Services.AddSingleton<ICommandRunner, CommandRunner>();
            builder.Services.AddSingleton<IAppSettingService, AppSettingService>();
            builder.Services.AddSingleton<IRecordingSessionService, RecordingSessionService>();

            // Validation
            builder.Services.AddSingleton<IFlowValidationService, FlowValidationService>();

            // Flow script
            builder.Services.AddSingleton<IPrinter, Printer>();
            builder.Services.AddSingleton<IParser, Parser>();
            builder.Services.AddSingleton<IFlowScriptExporter, FlowScriptExporter>();
            builder.Services.AddSingleton<IFlowScriptImporter, FlowScriptImporter>();

            builder.Services.AddExecutionEngine();

            // AI.
            // Scoped per call rather than singleton: the client is rebuilt from settings each time, so changing provider or key in Settings takes effect on the next question.
            builder.Services.AddScoped<IAiProviderService, AiProviderService>();
            builder.Services.AddScoped<IAiClientFactory, AiClientFactory>();
            builder.Services.AddScoped<IAiModelService, AiModelService>();
            builder.Services.AddScoped<IExecutionRunExplainService, ExecutionRunExplainService>();
            builder.Services.AddScoped<IFlowQuestionService, FlowQuestionService>();
            builder.Services.AddScoped<IExecutionScreenshotReader, ExecutionScreenshotReader>();
            builder.Services.AddSingleton<IAiModelDownloadService, AiModelDownloadService>();

            // Singletons, unlike the rest of AI: these hold the loaded onnx model and the built index, which cost seconds to produce and nothing to keep.
            builder.Services.AddSingleton<IEmbeddingService, OnnxEmbeddingService>();
            builder.Services.AddSingleton<IAiDocumentIndexService, AiDocumentIndexService>();

            // Ollama is on this machine, so a short timeout is right.
            builder.Services.AddHttpClient(nameof(AiModelService), client =>
            {
                client.Timeout = TimeSpan.FromSeconds(5);
            });

            // Infinite for downloading 9 gigabytes.
            builder.Services.AddHttpClient(nameof(AiModelDownloadService), client =>
            {
                client.Timeout = Timeout.InfiniteTimeSpan;
            });

            // Notifications.
            // The queue is a singleton because the throttle is per bot and has to be remembered between flows, not per request.
            builder.Services.AddHttpClient(nameof(DiscordNotifier), client =>
            {
                client.Timeout = TimeSpan.FromSeconds(15);
            });
            builder.Services.AddSingleton<IDiscordNotifier, DiscordNotifier>();
            builder.Services.AddSingleton<DiscordSendQueue>();
            builder.Services.AddSingleton<IDiscordSendQueue>(x => x.GetRequiredService<DiscordSendQueue>());


            // IPC
            builder.Services.AddSingleton<IpcRequestPipe>();
            builder.Services.AddSingleton<IpcBroadcastPipe>();
            builder.Services.AddSingleton<IpcDispatcher>();
            builder.Services.AddSingleton<IIpcBroadcastService, IpcBroadcastService>();
            builder.Services.AddHostedService<HostedRequestPipeListener>();// <- Background service!
            builder.Services.AddHostedService<HostedBroadcastPipeListener>();// <- Background service!

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
            builder.Services.AddTransient<ExtractSubFlowHandler>();
            builder.Services.AddTransient<GetFlowTreeNodeHandler>();
            builder.Services.AddTransient<ExportFlowHandler>();
            builder.Services.AddTransient<ImportFlowHandler>();
            builder.Services.AddTransient<CreateDiscordBotHandler>();
            builder.Services.AddTransient<UpdateDiscordBotHandler>();
            builder.Services.AddTransient<DeleteDiscordBotHandler>();
            builder.Services.AddTransient<GetDiscordBotHandler>();
            builder.Services.AddTransient<GetLazyDiscordBotHandler>();
            builder.Services.AddTransient<TestDiscordBotHandler>();
            builder.Services.AddTransient<CreateFlowStepHandler>();
            builder.Services.AddTransient<CreateFlowStepsHandler>();
            builder.Services.AddTransient<UpdateFlowStepHandler>();
            builder.Services.AddTransient<DeleteFlowStepHandler>();
            builder.Services.AddTransient<GetFlowStepHandler>();
            builder.Services.AddTransient<GetLazyFlowStepHandler>();
            builder.Services.AddTransient<GetFlowStepTreeNodeHandler>();
            builder.Services.AddTransient<GetFlowStepTreeNodesRecursiveHandler>();
            builder.Services.AddTransient<GetFlowStepDeleteImpactHandler>();
            builder.Services.AddTransient<GetFlowStepMovePreviewHandler>();
            builder.Services.AddTransient<MoveFlowStepHandler>();
            builder.Services.AddTransient<TestImageSearchHandler>();
            builder.Services.AddTransient<TestRunCommandHandler>();
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
            builder.Services.AddTransient<CreateFlowStepTemplateHandler>();
            builder.Services.AddTransient<GetFlowStepTemplateHandler>();
            builder.Services.AddTransient<GetLookupWindowHandler>();
            builder.Services.AddTransient<GetLookupMonitorHandler>();
            builder.Services.AddTransient<GetLookupFlowStepHandler>();
            builder.Services.AddTransient<GetLookupFlowPointHandler>();
            builder.Services.AddTransient<GetLookupSubFlowHandler>();
            builder.Services.AddTransient<GetLookupDiscordBotHandler>();
            builder.Services.AddTransient<GetLookupFailedStepHandler>();
            builder.Services.AddTransient<TestWindowMatchHandler>();
            builder.Services.AddTransient<GetLookupFlowAreaHandler>();
            builder.Services.AddTransient<GetLookupOcrLanguagesHandler>();
            builder.Services.AddTransient<GetLookupAiModelsHandler>();
            builder.Services.AddTransient<GetLookupAiModelSuggestionsHandler>();
            builder.Services.AddTransient<StartRecordingHandler>();
            builder.Services.AddTransient<StopRecordingHandler>();
            builder.Services.AddTransient<DiscardRecordingHandler>();
            builder.Services.AddTransient<GetRecordingScreenshotHandler>();
            builder.Services.AddTransient<GetAppSettingsHandler>();
            builder.Services.AddTransient<SetAppSettingHandler>();
            builder.Services.AddTransient<SystemTakeScreenshotHandler>();
            builder.Services.AddTransient<SystemCaptureForOverlayHandler>();
            builder.Services.AddTransient<SystemMoveCursorHandler>();
            builder.Services.AddTransient<SystemInstallOcrLanguageHandler>();
            builder.Services.AddTransient<SystemOpenWindowsLanguageSettingsHandler>();
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
            builder.Services.AddTransient<GetExecutionListHandler>();
            builder.Services.AddTransient<GetExecutionStateHandler>();
            builder.Services.AddTransient<GetFlowExecutionSummariesHandler>();
            builder.Services.AddTransient<GetExecutionStepScreenshotHandler>();
            builder.Services.AddTransient<ExplainExecutionHandler>();
            builder.Services.AddTransient<GetAiStatusHandler>();
            builder.Services.AddTransient<GetAiChatAvailabilityHandler>();
            builder.Services.AddTransient<AskAiHandler>();
            builder.Services.AddTransient<DownloadAiModelHandler>();
            builder.Services.AddTransient<GetAiDownloadStateHandler>();
            builder.Services.AddTransient<ClearAiDownloadStateHandler>();


            // SharpHook 
            builder.Services.AddHostedService<HostedSharpHookService>(); // <- Background service!
            builder.Services.AddHostedService<HostedAiDocumentIndexService>(); // <- Background service!

            // AutoMapper
            builder.Services.AddAutoMapper(config => config.AddProfile<AutoMapperProfile>());


            // Localization (JSON)
            //builder.Services.AddSingleton<IStringLocalizerFactory, JsonLocalizerFactory>();
            //builder.Services.AddTransient(typeof(IStringLocalizer), typeof(JsonLocalizer));


            IHost app = builder.Build();


            // Run migrations and seed data.
            using IServiceScope scope = app.Services.CreateScope();
            IDbContextFactory<AppDbContext> dbContectFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<AppDbContext>>();
            await using AppDbContext dbContext = await dbContectFactory.CreateDbContextAsync();
            dbContext.Database.Migrate();

            // Check if any exution is set as RUNNING and stop them.
            TimeProvider timeProvider = scope.ServiceProvider.GetRequiredService<TimeProvider>();
            await dbContext.Executions
                .Where(x => x.Status == ExecutionStatusEnum.RUNNING)
                .ExecuteUpdateAsync(x => x
                    .SetProperty(execution => execution.Status, ExecutionStatusEnum.ABANDONED)
                    .SetProperty(execution => execution.CompletedAt, timeProvider.GetUtcNow().UtcDateTime));

            await app.RunAsync();
        }
    }


    // TODO: move them from here

    // Main Pipe
    internal sealed class HostedRequestPipeListener : BackgroundService
    {
        private readonly IpcRequestPipe _ipcRequestPipe;
        public HostedRequestPipeListener(IpcRequestPipe ipcRequestPipe) => _ipcRequestPipe = ipcRequestPipe;
        protected override Task ExecuteAsync(CancellationToken cancellationToken) => _ipcRequestPipe.StartBackgroundService(cancellationToken);
    }

    // Broadcast Pipe
    internal sealed class HostedBroadcastPipeListener : BackgroundService
    {
        private readonly IpcBroadcastPipe _ipcBroadcastPipe;
        public HostedBroadcastPipeListener(IpcBroadcastPipe ipcBroadcastPipe) => _ipcBroadcastPipe = ipcBroadcastPipe;
        protected override Task ExecuteAsync(CancellationToken cancellationToken) => _ipcBroadcastPipe.StartBackgroundService(cancellationToken);
    }


    // Start global input recording hook.
    internal sealed class HostedSharpHookService : BackgroundService
    {
        private readonly IInputRecordService _inputRecordService;
        public HostedSharpHookService(IInputRecordService inputRecordService) => _inputRecordService = inputRecordService;
        protected override Task ExecuteAsync(CancellationToken cancellationToken) => _inputRecordService.StartGlobalHookAsync();
    }

    // Embed the docs at startup.
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
}
