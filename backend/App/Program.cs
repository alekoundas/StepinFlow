using App.Ipc;
using Business.DataService.Services;
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
            // Logging: Keep console, but filter out noisy EF logs
            //builder.Logging.ClearProviders();
            //builder.Logging.AddConsole(options =>
            //{
            //    options.IncludeScopes = true;
            //    options.LogToStandardErrorThreshold = LogLevel.Warning; // Only errors/warnings to stderr
            //});


            // DB context factory and Data service.
            builder.Services.AddCustomDbContextFactory();
            builder.Services.AddSingleton<IDataService, DataService>();

            // IPC
            builder.Services.AddSingleton<IpcDispatcher>();
            builder.Services.AddSingleton<IpcService>();
            builder.Services.AddHostedService(sp =>
            {
                var svc = sp.GetRequiredService<IpcService>();
                return new HostedPipeListener(svc);
            });

            // Localization (JSON)
            //builder.Services.AddSingleton<IStringLocalizerFactory, JsonLocalizerFactory>();
            //builder.Services.AddTransient(typeof(IStringLocalizer), typeof(JsonLocalizer));

            // IPC as Hosted Service (runs the stdin loop in background)
            //builder.Services.AddHostedService<IpcHostedService>();

            IHost app = builder.Build();


            // Run migrations and seed data
            using var scope = app.Services.CreateScope();
            var dbContectFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<AppDbContext>>();
            await using AppDbContext dbContext = await dbContectFactory.CreateDbContextAsync();
            dbContext.Database.Migrate();

            Console.WriteLine("[.NET] - DB ready");


            //var handler = new IpcService();
            //_ = Task.Run(() => new IpcService().StartListening());
            //_ = Task.Run(() => new IpcService().StartListeningDebug());
            Console.WriteLine("[.NET] - Pipe listener started");
            await app.RunAsync();
        }
    }

    internal class HostedPipeListener : BackgroundService
    {
        private readonly IpcService _ipc;
        public HostedPipeListener(IpcService ipc) => _ipc = ipc;
        protected override Task ExecuteAsync(CancellationToken stoppingToken)
            => _ipc.StartAsync(stoppingToken);
    }
}
