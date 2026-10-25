using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace AdlpLib.Persistence;

/// <summary>Applies pending migrations, serializing calls within this process.</summary>
public static class AdlpDatabaseInitializer
{
    private static readonly SemaphoreSlim MigrationLock = new(1, 1);

    public static async Task ApplyPendingMigrationsAsync(
        AdlpDbContext db, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(db);
        await MigrationLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            // Do not use EnsureCreatedAsync: it bypasses EF's migration history.
            await db.Database.MigrateAsync(cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            MigrationLock.Release();
        }
    }
}
