using Business.Helpers;
using Business.Configurations;
using Microsoft.EntityFrameworkCore;
using Model.Models;
using Model.Enums;
using System.Windows;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore.Diagnostics;
using System.Data.Common;

namespace Business.DatabaseContext
{
    public class InMemoryDbContext : DbContext
    {
        public DbSet<Flow> Flows { get; set; }
        public DbSet<FlowStep> FlowSteps { get; set; }
        public DbSet<FlowParameter> FlowParameters { get; set; }
        public DbSet<Execution> Executions { get; set; }
        public DbSet<AppSetting> AppSettings { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            string dataPath = PathHelper.GetDatabaseDataPath();
            string dataSource = Path.Combine(dataPath, "StepinFlowDatabase.db");

            // Create the directory if it doesn’t exist
            if (!Directory.Exists(dataPath))
                Directory.CreateDirectory(dataPath);

            // Create the database file if it doesn’t exist.
            if (!File.Exists(dataSource))
                File.Create(dataSource).Close();

            //optionsBuilder.UseSqlite($"Data Source={dataSource};");
            optionsBuilder.UseSqlite($"Data Source={dataSource};").AddInterceptors(new SQLiteBusyTimeoutInterceptor());

            //optionsBuilder.UseSqlite($"Data Source={dataSource}", sqliteOptions =>
            //{
            //    sqliteOptions..ConnectionOpened(dbConnection =>
            //    {
            //        using var cmd = dbConnection.CreateCommand();
            //        cmd.CommandText = "PRAGMA busy_timeout = 10000;"; // 10 seconds
            //        cmd.ExecuteNonQuery();
            //    });
            //});

            optionsBuilder.EnableSensitiveDataLogging();
            optionsBuilder.EnableDetailedErrors();
        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);


            builder.ApplyConfiguration(new FlowConfiguration());
            builder.ApplyConfiguration(new FlowStepConfiguration());
            builder.ApplyConfiguration(new ExecutionConfiguration());
            builder.ApplyConfiguration(new AppSettingConfiguration());
            builder.ApplyConfiguration(new FlowParameterConfiguration());
        }

        public void RunMigrations()
        {
            this.Database.Migrate();
        }

        public void EnableWAL()
        {
            string dataPath = PathHelper.GetDatabaseDataPath();
            string dataSource = Path.Combine(dataPath, "StepinFlowDatabase.db");
            using (var connection = new SqliteConnection("Data Source={dataSource}"))
            {
                connection.Open();
                var command = connection.CreateCommand();
                command.CommandText = "PRAGMA journal_mode=WAL;";
                command.ExecuteNonQuery();
            }
        }
        public void EnableBusyTimeout()
        {
            //string dataPath = PathHelper.GetDatabaseDataPath();
            //string dataSource = Path.Combine(dataPath, "StepinFlowDatabase.db");
            //using (var connection = new SqliteConnection("Data Source={dataSource}"))
            //{
            //    connection.Open();
            //    var command = connection.CreateCommand();
            //    command.CommandText = "PRAGMA busy_timeout = 10000;"; // 10 seconds
            //    command.ExecuteNonQuery();
            //}
        }


        public void TrySeedInitialData()
        {
            // Main window location and size.
            if (!AppSettings.Any(x => x.Key == AppSettingsEnum.MAIN_WINDOW_LEFT))
                AppSettings.Add(new AppSetting { Key = AppSettingsEnum.MAIN_WINDOW_LEFT, Value = ((SystemParameters.PrimaryScreenWidth - 1000) / 2).ToString() });

            if (!AppSettings.Any(x => x.Key == AppSettingsEnum.MAIN_WINDOW_TOP))
                AppSettings.Add(new AppSetting { Key = AppSettingsEnum.MAIN_WINDOW_TOP, Value = ((SystemParameters.PrimaryScreenHeight - 600) / 2).ToString() });

            if (!AppSettings.Any(x => x.Key == AppSettingsEnum.MAIN_WINDOW_WIDTH))
                AppSettings.Add(new AppSetting { Key = AppSettingsEnum.MAIN_WINDOW_WIDTH, Value = "1000" });

            if (!AppSettings.Any(x => x.Key == AppSettingsEnum.MAIN_WINDOW_HEIGHT))
                AppSettings.Add(new AppSetting { Key = AppSettingsEnum.MAIN_WINDOW_HEIGHT, Value = "600" });

            if (!AppSettings.Any(x => x.Key == AppSettingsEnum.IS_MAIN_WINDOW_MAXIMIZED))
                AppSettings.Add(new AppSetting { Key = AppSettingsEnum.IS_MAIN_WINDOW_MAXIMIZED, Value = "false" });


            // Selector window location and size.
            if (!AppSettings.Any(x => x.Key == AppSettingsEnum.SELECTOR_WINDOW_LEFT))
                AppSettings.Add(new AppSetting { Key = AppSettingsEnum.SELECTOR_WINDOW_LEFT, Value = ((SystemParameters.PrimaryScreenWidth - 1000) / 2).ToString() });

            if (!AppSettings.Any(x => x.Key == AppSettingsEnum.SELECTOR_WINDOW_TOP))
                AppSettings.Add(new AppSetting { Key = AppSettingsEnum.SELECTOR_WINDOW_TOP, Value = ((SystemParameters.PrimaryScreenHeight - 600) / 2).ToString() });

            if (!AppSettings.Any(x => x.Key == AppSettingsEnum.SELECTOR_WINDOW_WIDTH))
                AppSettings.Add(new AppSetting { Key = AppSettingsEnum.SELECTOR_WINDOW_WIDTH, Value = "1000" });

            if (!AppSettings.Any(x => x.Key == AppSettingsEnum.SELECTOR_WINDOW_HEIGHT))
                AppSettings.Add(new AppSetting { Key = AppSettingsEnum.SELECTOR_WINDOW_HEIGHT, Value = "600" });

            if (!AppSettings.Any(x => x.Key == AppSettingsEnum.IS_SELECTOR_WINDOW_MAXIMIZED))
                AppSettings.Add(new AppSetting { Key = AppSettingsEnum.IS_SELECTOR_WINDOW_MAXIMIZED, Value = "false" });


            // Execution history log settings.
            if (!AppSettings.Any(x => x.Key == AppSettingsEnum.EXECUTION_HISTORY_LOG_IMAGE_QUALITY))
                AppSettings.Add(new AppSetting { Key = AppSettingsEnum.EXECUTION_HISTORY_LOG_IMAGE_QUALITY, Value = "80" });

            if (!AppSettings.Any(x => x.Key == AppSettingsEnum.IS_EXECUTION_HISTORY_LOG_ENABLED))
                AppSettings.Add(new AppSetting { Key = AppSettingsEnum.IS_EXECUTION_HISTORY_LOG_ENABLED, Value = "true" });

            // Theme settings.
            if (!AppSettings.Any(x => x.Key == AppSettingsEnum.IS_THEME_DARK))
                AppSettings.Add(new AppSetting { Key = AppSettingsEnum.IS_THEME_DARK, Value = "true" });



            this.SaveChanges();
        }
    }

    public class SQLiteBusyTimeoutInterceptor : IDbConnectionInterceptor
    {
        void ConnectionOpened(DbConnection connection, ConnectionEndEventData eventData)
        {
            using var cmd = connection.CreateCommand();
            cmd.CommandText = "PRAGMA busy_timeout = 10000;"; // 10 seconds
            cmd.ExecuteNonQuery();
        }

        public void ConnectionOpening(DbConnection connection, ConnectionEventData eventData, InterceptionResult result)
        {
        }
        public void ConnectionClosed(DbConnection connection, ConnectionEndEventData eventData) { }
        public void ConnectionClosing(DbConnection connection, ConnectionEventData eventData, InterceptionResult result) { }

    }
}
