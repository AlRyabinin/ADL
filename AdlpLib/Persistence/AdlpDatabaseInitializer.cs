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
            bool mysql = db.Database.ProviderName == "Pomelo.EntityFrameworkCore.MySql";
            if(mysql)
            {
                // Держим одно соединение открытым, чтобы SET действовал на все DDL миграций.
                await db.Database.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
                await db.Database.ExecuteSqlRawAsync(
                    "SET SESSION default_storage_engine = InnoDB;", cancellationToken).ConfigureAwait(false);
            }
            try
            {
                await db.Database.MigrateAsync(cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                if(mysql) await db.Database.CloseConnectionAsync().ConfigureAwait(false);
            }
        }
        finally
        {
            MigrationLock.Release();
        }
    }
}
