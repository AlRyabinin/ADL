using Microsoft.Data.Sqlite;
using MySql.Data.MySqlClient;
using System;
using System.Data.Common;

namespace AdlpLib.Configuration.Application;

/// <summary>
/// Утилита для проверки доступности базы данных по текущей конфигурации.
/// </summary>
public static class DatabaseTester
{
    /// <summary>
    /// Пытается открыть соединение и выполнить тривиальный запрос.
    /// </summary>
    public static (bool ok, string message) Test(AppConfig cfg)
    {
        try
        {
            var cs = AppConfigService.BuildConnectionString(cfg);

            // Создаём соединение в зависимости от типа СУБД
            using var conn = CreateConnection(cfg.DatabaseType, cs);
            conn.Open();

            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT 1";
            cmd.ExecuteScalar();

            return (true, "Подключение успешно.");
        }
        catch(Exception ex)
        {
            return (false, ex.Message);
        }
    }

    /// <summary>
    /// Создаёт объект соединения для указанного типа СУБД.
    /// </summary>
    private static DbConnection CreateConnection(DatabaseType type, string connectionString) => type switch
    {
        DatabaseType.SQLite => new SqliteConnection(connectionString),
        DatabaseType.MySQL => new MySqlConnection(connectionString),
        _ => throw new NotSupportedException($"Тип БД {type} не поддержан.")
    };
}