
using Api.AutoMapper;
using DataAccess;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;
using System.Text.Json.Serialization;

namespace Api
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.

            builder.Services.AddControllers();
            builder.Services.AddOpenApi(); 

            builder.Services.AddSignalR();

            // DB context and factory.
            builder.Services.AddCustomDbContextFactory();

            builder.Services.AddAutoMapper(config => config.AddProfile<AutoMapperProfile>());

            builder.Services
            .AddControllers(options =>
            {
                // Add custom validation filter
                //options.Filters.Add<ValidateModelFilter>();
            })
            .AddJsonOptions(x =>
            {
                // Serialize enums as strings in api responses (e.g. Role).
                //x.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());

                // Ignore omitted parameters on models to enable optional params (e.g. User update).
                //x.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;

                // Ignore circular references.
                x.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
            }).AddDataAnnotationsLocalization();


            // Optional: Add CORS – very permissive for local desktop app
            //builder.Services.AddCors(options =>
            //{
            //    options.AddDefaultPolicy(policy =>
            //    {
            //        policy.AllowAnyOrigin()                     // Simplest & safe for offline/local-only
            //              .AllowAnyMethod()
            //              .AllowAnyHeader()
            //              .AllowCredentials();                  // If you ever use cookies/auth (rare here)
            //    });
            //});

            // If you want to customize Scalar title/theme/etc.
            //builder.Services.AddScalarApiReference(options =>
            //{
            //    options
            //        .WithTitle("StepinFlow Local API")
            //        .WithTheme(ScalarTheme.DeepSpace)           // Nice dark theme – or Mars, Kepler, etc.
            //        .WithDefaultHttpClient(ScalarHttpClient.Fetch); // or other options
            //});

            var app = builder.Build();


            // Run migrations and seed data
            using var scope = app.Services.CreateScope();
            var dbContectFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<ApiDbContext>>();
            await using ApiDbContext dbContext = await dbContectFactory.CreateDbContextAsync();
            dbContext.Database.Migrate();


            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.MapOpenApi();
                // http://localhost:5220/scalar/v1
                app.MapScalarApiReference(); // This will add the /scalar-api-reference endpoint to serve the Scalar API reference documentation
            }

            //app.UseCors();
            app.UseAuthorization();

            //app.MapHub<UpdatesHub>("/hub/updates");

            app.MapControllers();
            app.Run();
        }
    }
}
