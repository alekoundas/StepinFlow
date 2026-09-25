using DataAccess;

using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Business.Tests.Fakes
{
    /// <summary>
    /// A real SQLite database in memory, one per test, built by the real migrations - so a broken
    /// migration fails a test rather than a user.
    ///
    /// The database lives inside the connection, so the one connection is held open for the test
    /// and handed to every context. Given a connection string instead, each context would open
    /// its own empty database.
    /// </summary>
    public sealed class TestDatabase : IDbContextFactory<AppDbContext>, IDisposable
    {
        private readonly SqliteConnection _connection;
        private readonly DbContextOptions<AppDbContext> _options;

        public TestDatabase()
        {
            _connection = new SqliteConnection("Data Source=:memory:");
            _connection.Open();

            _options = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlite(_connection)
                .Options;

            using AppDbContext db = CreateDbContext();
            db.Database.Migrate();
        }

        public AppDbContext CreateDbContext()
        {
            return new AppDbContext(_options);
        }

        public void Dispose()
        {
            _connection.Dispose();
        }
    }
}
