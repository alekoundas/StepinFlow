using Core.Models;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace DataAccess.Interceptors
{
    /// <summary>
    /// Stamps CreatedOn and UpdatedOn on the way to the database. 
    /// </summary>
    public sealed class TimestampInterceptor : SaveChangesInterceptor
    {
        private readonly TimeProvider _timeProvider;

        public TimestampInterceptor(TimeProvider timeProvider)
        {
            _timeProvider = timeProvider;
        }


        // ================================================================
        // Public methods
        // ================================================================

        public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
        {
            Stamp(eventData.Context);

            return base.SavingChanges(eventData, result);
        }

        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            Stamp(eventData.Context);

            return base.SavingChangesAsync(eventData, result, cancellationToken);
        }


        // ================================================================
        // Private methods
        // ================================================================

        // One read of the clock per save, so everything written together carries the same instant.
        private void Stamp(DbContext? dbContext)
        {
            if (dbContext == null)
                return;

            DateTime now = _timeProvider.GetUtcNow().UtcDateTime;

            foreach (EntityEntry<BaseDbModel> entry in dbContext.ChangeTracker.Entries<BaseDbModel>())
            {
                if (entry.State == EntityState.Added)
                    entry.Entity.CreatedOn = now;
                else if (entry.State == EntityState.Modified)
                    entry.Entity.UpdatedOn = now;
            }
        }
    }
}
