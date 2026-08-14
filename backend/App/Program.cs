using App.AutoMapper;
using App.Ipc;
using Business.Helpers;
using Business.Ipc.Handlers;
using Business.Services.CommandService;
using Business.Services.FrameService;
using Business.Services.InputService;
using Business.Services.MatchService;
using Business.Services.OcrService;
using Business.Services.ScreenshotService;
using Business.Services.SystemActionService;
using Core.Interfaces;
using DataAccess;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

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
            builder.Services.AddSingleton<IFrameResolver, FrameResolver>();
            builder.Services.AddSingleton<ITemplateMatcher, TemplateMatcher>();
            builder.Services.AddSingleton<IInputService, InputService>();
            builder.Services.AddSingleton<IScreenshotService, ScreenshotService>();
            builder.Services.AddSingleton<IInputRecordService, InputRecordService>();
            builder.Services.AddSingleton<IWindowsGraphicsCaptureService, WindowsGraphicsCaptureService>();
            builder.Services.AddSingleton<ICommandRunner, CommandRunner>();
            builder.Services.AddSingleton<ISystemActionService, SystemActionService>();
            builder.Services.AddSingleton<IOcrService, OcrService>();


            // IPC
            builder.Services.AddSingleton<IpcRequestPipe>();
            builder.Services.AddSingleton<IpcBroadcastPipe>();
            builder.Services.AddSingleton<IpcDispatcher>();
            builder.Services.AddSingleton<IIpcBroadcastService, IpcBroadcastService>();
            builder.Services.AddHostedService<HostedRequestPipeListener>();// <- Background service!
            builder.Services.AddHostedService<HostedBroadcaststPipeListener>();// <- Background service!

            // SharpHook 
            builder.Services.AddHostedService<HostedSharpHookService>(); // <- Background service!

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

    // Start global input recording hook.
    internal class HostedSharpHookService : BackgroundService
    {
        private readonly IInputRecordService _inputRecordService;
        public HostedSharpHookService(IInputRecordService inputRecordService) => _inputRecordService = inputRecordService;
        protected override Task ExecuteAsync(CancellationToken cancellationToken) => _inputRecordService.StartGlobalHookAsync();
    }

}
