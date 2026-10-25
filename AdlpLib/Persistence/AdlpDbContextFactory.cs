using System;
using System.IO;
using AdlpLib.Configuration.Application;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Pomelo.EntityFrameworkCore.MySql.Infrastructure;

namespace AdlpLib.Persistence;

public static class AdlpDbContextFactory
{
    public static AdlpDbContext Create(string connectionString, DatabaseType databaseType)
    {
        var options = new DbContextOptionsBuilder<AdlpDbContext>();
        switch (databaseType)
        {
            case DatabaseType.SQLite:
                var sqliteConnection = new SqliteConnectionStringBuilder(connectionString)
                {
                    ForeignKeys = true
                }.ToString();
                options.UseSqlite(sqliteConnection,
                    x => x.MigrationsAssembly(typeof(AdlpDbContext).Assembly.FullName));
                break;
            case DatabaseType.MySQL:
                options.UseMySql(connectionString, new MySqlServerVersion(new Version(8, 0, 0)),
                    x => x.MigrationsAssembly(typeof(AdlpDbContext).Assembly.FullName));
                break;
            default:
                throw new NotSupportedException($"Database type {databaseType} is not supported.");
        }
        return new AdlpDbContext(options.Options);
    }
}

public sealed class AdlpDesignTimeDbContextFactory : IDesignTimeDbContextFactory<AdlpDbContext>
{
    public AdlpDbContext CreateDbContext(string[] args)
    {
        var provider = Environment.GetEnvironmentVariable("ADLP_DB_PROVIDER") ?? "SQLite";
        var connectionString = Environment.GetEnvironmentVariable("ADLP_CONNECTION_STRING");
        if (string.Equals(provider, "MySQL", StringComparison.OrdinalIgnoreCase))
        {
            connectionString ??= "Server=localhost;Port=3306;Database=adlp;User=root;Password=;AllowUserVariables=True";
            return AdlpDbContextFactory.Create(connectionString, DatabaseType.MySQL);
        }
        var databasePath = Environment.GetEnvironmentVariable("ADLP_SQLITE_PATH")
            ?? Path.Combine(Path.GetTempPath(), "adlp-design-time.db");
        connectionString ??= $"Data Source={databasePath};";
        return AdlpDbContextFactory.Create(connectionString, DatabaseType.SQLite);
    }
}
