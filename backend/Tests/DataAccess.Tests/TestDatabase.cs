using DataAccess.Interceptors;

using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace DataAccess.Tests
{
    /// <summary>
    /// A real SQLite database in memory, one per test, built by the real migrations. 
    /// The database lives inside the connection, so one connection is held open and handed to every context.
    /// NO DISK FILE
    /// </summary>
    public sealed class TestDatabase : IDisposable
    {
        private readonly SqliteConnection _connection;
        private readonly DbContextOptions<AppDbContext> _options;

        public TestDatabase(TimeProvider? clock = null)
        {
            _connection = new SqliteConnection("Data Source=:memory:");
            _connection.Open();

            DbContextOptionsBuilder<AppDbContext> builder = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(_connection);
            if (clock != null)
                builder.AddInterceptors(new TimestampInterceptor(clock));

            _options = builder.Options;

            using AppDbContext db = Open();
            db.Database.Migrate();
        }

        public AppDbContext Open()
        {
            return new AppDbContext(_options);
        }

        public void Dispose()
        {
            _connection.Dispose();
        }
    }
}
