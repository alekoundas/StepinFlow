// CreatedOn moved from a property initializer to the interceptor, so it is now set at save rather
// than at new. If the interceptor is not wired up, every row silently gets 0001-01-01.
using Core.Models.Database;

using DataAccess;
using DataAccess.Interceptors;

using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

// A clock that is obviously not the wall clock, so a passing test cannot be a coincidence.
FakeClock clock = new FakeClock(new DateTimeOffset(2031, 4, 5, 6, 7, 8, TimeSpan.Zero));

using SqliteConnection connection = new SqliteConnection("Data Source=:memory:");
connection.Open();

DbContextOptions<AppDbContext> options = new DbContextOptionsBuilder<AppDbContext>()
    .UseSqlite(connection)
    .AddInterceptors(new TimestampInterceptor(clock))
    .Options;

int failed = 0;

void Assert(string what, bool ok, string detail)
{
    if (!ok)
        failed++;
    Console.WriteLine($"  {(ok ? "ok  " : "FAIL")}  {what}: {detail}");
}

using (AppDbContext db = new AppDbContext(options))
{
    db.Database.EnsureCreated();

    Flow flow = new Flow { Name = "probe" };
    Assert("unsaved entity carries no timestamp", flow.CreatedOn == default, flow.CreatedOn.ToString("O"));

    db.Flows.Add(flow);
    await db.SaveChangesAsync();

    Assert("CreatedOn stamped on insert", flow.CreatedOn == clock.Now.UtcDateTime, flow.CreatedOn.ToString("O"));
    Assert("UpdatedOn untouched on insert", flow.UpdatedOn == null, flow.UpdatedOn?.ToString("O") ?? "null");
}

using (AppDbContext db = new AppDbContext(options))
{
    Flow flow = await db.Flows.FirstAsync();
    Assert("CreatedOn survives the round trip", flow.CreatedOn == clock.Now.UtcDateTime, flow.CreatedOn.ToString("O"));

    clock.Advance(TimeSpan.FromHours(3));

    flow.Name = "probe edited";
    await db.SaveChangesAsync();

    Assert("UpdatedOn stamped on modify", flow.UpdatedOn == clock.Now.UtcDateTime, flow.UpdatedOn?.ToString("O") ?? "null");
    Assert("CreatedOn not re-stamped", flow.CreatedOn != clock.Now.UtcDateTime, flow.CreatedOn.ToString("O"));
}

Console.WriteLine();
Console.WriteLine(failed == 0 ? "INTERCEPTOR WORKS" : $"{failed} FAILED");
return failed;


internal sealed class FakeClock : TimeProvider
{
    public FakeClock(DateTimeOffset now)
    {
        Now = now;
    }

    public DateTimeOffset Now { get; private set; }

    public void Advance(TimeSpan by)
    {
        Now += by;
    }

    public override DateTimeOffset GetUtcNow()
    {
        return Now;
    }
}
