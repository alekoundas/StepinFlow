using App.AutoMapper;
using App.Ipc;
using Business.Helpers;
using Business.Ipc.Handlers;
using Business.Services.CommandService;
using Business.Services.AreaPointService;
using Business.Services.FlowValidationService;
using Business.Services.InputService;
using Business.Services.MatchService;
using Business.Services.Ai;
using Business.Services.AppSettingService;
using Business.Services.OcrService;
using Business.Services.RecordingService;
using Business.Services.ScreenshotService;
using Business.Services.SystemActionService;
using Core.Interfaces;
using DataAccess;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Business.Services.NotificationService;
using App.DependencyInjection;
using Core.Enums;
using Business.Services.Ai.Providers;
using Business.Services.Ai.AiDocuments;
using Business.Services.Ai.AiModels;

namespace App
{
    internal class Program
    {
        public static async Task Main(string[] args)
        {
            // First thing, before any coordinate API: without it Windows virtualizes every rect
            // to 96 DPI and nothing matches the capture buffers or the input hook.
            ScreenHelper.EnablePerMonitorDpiAwareness();

            HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);

            // Logging
            builder.Logging.ClearProviders();
            builder.Logging.AddConsole();


            // DB context factory. Handlers own their DbContext and their own write statements.
            builder.Services.AddCustomDbContextFactory();


            // Services
            builder.Services.AddSingleton<IAreaPointResolver, AreaPointResolver>();
            builder.Services.AddSingleton<IOpenCvService, OpenCvService>();
            builder.Services.AddSingleton<IInputService, InputService>();
            builder.Services.AddSingleton<IScreenshotService, ScreenshotService>();
            builder.Services.AddSingleton<IInputRecordService, InputRecordService>();
            builder.Services.AddSingleton<IWindowsGraphicsCaptureService, WindowsGraphicsCaptureService>();
            builder.Services.AddSingleton<ICommandRunner, CommandRunner>();
            builder.Services.AddSingleton<ISystemActionService, SystemActionService>();
            builder.Services.AddSingleton<IOcrService, OcrService>();
            builder.Services.AddSingleton<IFlowValidator, FlowValidator>();
            builder.Services.AddSingleton<IAppSettingService, AppSettingService>();
            builder.Services.AddSingleton<IRecordingSessionService, RecordingSessionService>();

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
            builder.Services.AddHostedService<HostedBroadcaststPipeListener>();// <- Background service!

            // SharpHook 
            builder.Services.AddHostedService<HostedSharpHookService>(); // <- Background service!

            builder.Services.AddHostedService<HostedAiDocumentIndexService>(); // <- Background service!

            // MediatR
            builder.Services.AddMediatR(cfg =>
            {
                cfg.RegisterServicesFromAssembly(typeof(SystemTakeScreenshotHandler).Assembly); // scans all handlers in Business
            });


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
            await dbContext.Executions
                .Where(x => x.Status == ExecutionStatusEnum.RUNNING)
                .ExecuteUpdateAsync(x => x
                    .SetProperty(execution => execution.Status, ExecutionStatusEnum.ABANDONED)
                    .SetProperty(execution => execution.CompletedAt, DateTime.UtcNow));

            await app.RunAsync();
        }
    }


    // TODO: move them from here
    internal class HostedRequestPipeListener : BackgroundService
    {
        private readonly IpcRequestPipe _ipcRequestPipe;
        public HostedRequestPipeListener(IpcRequestPipe ipcRequestPipe) => _ipcRequestPipe = ipcRequestPipe;
        protected override Task ExecuteAsync(CancellationToken cancellationToken) => _ipcRequestPipe.StartBackgroundService(cancellationToken);
    }
    internal class HostedBroadcaststPipeListener : BackgroundService
    {
        private readonly IpcBroadcastPipe _ipcBroadcastPipe;
        public HostedBroadcaststPipeListener(IpcBroadcastPipe ipcBroadcastPipe) => _ipcBroadcastPipe = ipcBroadcastPipe;
        protected override Task ExecuteAsync(CancellationToken cancellationToken) => _ipcBroadcastPipe.StartBackgroundService(cancellationToken);
    }

    // Embed the docs at startup.
    internal class HostedAiDocumentIndexService : BackgroundService
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

    // Start global input recording hook.
    internal class HostedSharpHookService : BackgroundService
    {
        private readonly IInputRecordService _inputRecordService;
        public HostedSharpHookService(IInputRecordService inputRecordService) => _inputRecordService = inputRecordService;
        protected override Task ExecuteAsync(CancellationToken cancellationToken) => _inputRecordService.StartGlobalHookAsync();
    }

}
