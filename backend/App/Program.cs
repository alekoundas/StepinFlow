using App.Localization;
using Business.DataService.Services;
using DataAccess;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;

namespace App
{
    internal class Program
    {
        public static async Task Main(string[] args)
        {
            HostApplicationBuilder builder = Host.CreateApplicationBuilder (args);


            // Logging
            builder.Logging.ClearProviders();
            builder.Logging.AddConsole();


            // DB context factory and Data service.
            builder.Services.AddCustomDbContextFactory();
            builder.Services.AddSingleton<IDataService, DataService>();


            // Localization (JSON)
            builder.Services.AddSingleton<IStringLocalizerFactory, JsonLocalizerFactory>();
            builder.Services.AddTransient(typeof(IStringLocalizer), typeof(JsonLocalizer));

            // IPC as Hosted Service (runs the stdin loop in background)
            //builder.Services.AddHostedService<IpcHostedService>();

            IHost app = builder.Build();


            // Run migrations and seed data
            using var scope = app.Services.CreateScope();
            var dbContectFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<ApiDbContext>>();
            await using ApiDbContext dbContext = await dbContectFactory.CreateDbContextAsync();
            dbContext.Database.Migrate();


            await app.RunAsync();
        }
    }
}
