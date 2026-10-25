using System;
using AdlpLib.Persistence;

namespace AdlpLib.Configuration.Application;

public static class DatabaseTester
{
    public static (bool ok, string message) Test(AppConfig cfg)
    {
        try
        {
            var cs = AppConfigService.BuildConnectionString(cfg);

            using var db =
                AdlpDbContextFactory.Create(cs, cfg.DatabaseType);

            return db.Database.CanConnect()
                ? (true, "Подключение успешно.")
                : (false, "Не удалось подключиться к базе данных.");
        }
        catch(Exception ex)
        {
            return (false, ex.Message);
        }
    }
}