using Core.Models.Database;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Time.Testing;

namespace DataAccess.Tests
{
    /// <summary>
    /// CreatedOn is stamped on the way to the database, not when the object is made. Without the
    /// interceptor every row would silently be 0001-01-01. The clock is set to 2031 so a pass cannot
    /// be the wall clock by coincidence.
    /// </summary>
    public sealed class TimestampInterceptorTests : IDisposable
    {
        private static readonly DateTime Start = new DateTime(2031, 4, 5, 6, 7, 8, DateTimeKind.Utc);

        private readonly FakeTimeProvider _clock = new FakeTimeProvider(new DateTimeOffset(Start));
        private readonly TestDatabase _database;

        public TimestampInterceptorTests()
        {
            _database = new TestDatabase(_clock);
        }

        private static CancellationToken Ct
        {
            get { return TestContext.Current.CancellationToken; }
        }

        public void Dispose()
        {
            _database.Dispose();
        }

        private async Task<int> InsertAsync()
        {
            await using AppDbContext db = _database.Open();
            Flow flow = new Flow { Name = "Login" };

            flow.CreatedOn.ShouldBe(default);

            db.Flows.Add(flow);
            await db.SaveChangesAsync(Ct);
            return flow.Id;
        }

        [Fact]
        public async Task A_new_row_is_stamped_when_it_is_saved_and_not_before()
        {
            int id = await InsertAsync();

            await using AppDbContext db = _database.Open();
            Flow flow = await db.Flows.SingleAsync(x => x.Id == id, Ct);

            flow.CreatedOn.ShouldBe(Start);
            flow.UpdatedOn.ShouldBeNull();
        }

        [Fact]
        public async Task A_change_stamps_UpdatedOn_and_leaves_CreatedOn_alone()
        {
            int id = await InsertAsync();
            _clock.Advance(TimeSpan.FromHours(3));

            await using (AppDbContext db = _database.Open())
            {
                Flow flow = await db.Flows.SingleAsync(x => x.Id == id, Ct);
                flow.Name = "Login, edited";
                await db.SaveChangesAsync(Ct);
            }

            await using AppDbContext reread = _database.Open();
            Flow saved = await reread.Flows.SingleAsync(x => x.Id == id, Ct);

            saved.CreatedOn.ShouldBe(Start);
            saved.UpdatedOn.ShouldBe(Start.AddHours(3));
        }
    }
}
