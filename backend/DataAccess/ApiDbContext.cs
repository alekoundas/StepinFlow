using Core.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace DataAccess
{
    public class ApiDbContext : DbContext
    {
        private readonly IConfiguration _configuration;

        public ApiDbContext(DbContextOptions<ApiDbContext> options, IConfiguration configuration)
        : base(options)
        {
            _configuration = configuration;
        }

        //public DbSet<Mail> Mails { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            base.OnConfiguring(optionsBuilder);

            string dataPath = PathHelper.GetDatabaseDataPath();
            string dataSource = Path.Combine(dataPath, "StepinFlowSQLite.db");

            // Create the database file if it doesn’t exist.
            if (!File.Exists(dataSource))
                File.Create(dataSource).Close();

            optionsBuilder.UseSqlite($"Data Source={dataSource};");


            optionsBuilder.EnableSensitiveDataLogging();
            optionsBuilder.EnableDetailedErrors();
        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            //builder.ApplyConfiguration(new MailConfiguration());
        }

        //public void RunMigrations()
        //{
        //    this.Database.Migrate();
        //}
    }
}
