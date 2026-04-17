using App.AutoMapper;
using App.Ipc;
using Business.DataService.Services;
using Business.Ipc.Handlers;
using Business.Services.InputService;
using Business.Services.ScreenshotService;
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
            HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);

            // Logging
            builder.Logging.ClearProviders();
            builder.Logging.AddConsole();


            // DB context factory and Data service.
            builder.Services.AddCustomDbContextFactory();
            builder.Services.AddSingleton<IDataService, DataService>();


            // Services
            builder.Services.AddSingleton<IInputService, InputService>();
            builder.Services.AddSingleton<IScreenshotService, ScreenshotService>();
            builder.Services.AddSingleton<IInputRecordService, InputRecordService>();
            builder.Services.AddSingleton<IWindowsGraphicsCaptureService, WindowsGraphicsCaptureService>();


            // IPC
            builder.Services.AddSingleton<IpcService>();
            builder.Services.AddSingleton<IpcDispatcher>();
            builder.Services.AddHostedService(sp =>
            {
                var svc = sp.GetRequiredService<IpcService>();
                return new HostedPipeListener(svc);
            });


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


            // Run migrations and seed data
            using var scope = app.Services.CreateScope();
            var dbContectFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<AppDbContext>>();
            await using AppDbContext dbContext = await dbContectFactory.CreateDbContextAsync();
            dbContext.Database.Migrate();


            await app.RunAsync();
        }
    }

    internal class HostedPipeListener : BackgroundService
    {
        private readonly IpcService _ipc;
        public HostedPipeListener(IpcService ipc) => _ipc = ipc;
        protected override Task ExecuteAsync(CancellationToken cancellationToken)
            => _ipc.StartAsync(cancellationToken);
    }
}
