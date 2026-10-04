using MySql.Data.MySqlClient;
using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AdlpLib.Configuration.Application
{

    /// <summary>
    /// Сервис чтения/записи конфигурации приложения.
    /// Файл хранится в <c>%ProgramData%\MyCompany\MyApp\appsettings.json</c>.
    /// </summary>
    public static class AppConfigService
    {
        private const string CompanyFolder = "ADL";
        private const string AppFolder = "Adlp";

        /// <summary>
        /// Каталог, в котором хранится файл конфигурации.
        /// </summary>
        public static string ConfigDirectory { get; } = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
            CompanyFolder, AppFolder);

        /// <summary>
        /// Полный путь к файлу конфигурации.
        /// </summary>
        public static string ConfigFilePath { get; } =
            Path.Combine(ConfigDirectory, "Configuration.json");

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true,
            Converters = { new JsonStringEnumConverter() }
        };

        /// <summary>
        /// Загружает конфигурацию из файла. Если файл отсутствует — возвращает конфигурацию по умолчанию
        /// (без записи на диск).
        /// </summary>
        /// <returns>Загруженная или созданная по умолчанию конфигурация.</returns>
        /// <exception cref="IOException">Ошибка чтения файла.</exception>
        /// <exception cref="JsonException">Файл содержит некорректный JSON.</exception>
        public static AppConfig Load()
        {
            Directory.CreateDirectory(ConfigDirectory);
            if(!File.Exists(ConfigFilePath)) return CreateDefault();

            var json = File.ReadAllText(ConfigFilePath);
            var cfg = JsonSerializer.Deserialize<AppConfig>(json, JsonOptions) ?? CreateDefault();
            Normalize(cfg);
            return cfg;
        }

        /// <summary>
        /// Сохраняет конфигурацию на диск, создавая каталог при необходимости.
        /// </summary>
        /// <param name="config">Сохраняемая конфигурация.</param>
        /// <exception cref="UnauthorizedAccessException">Нет прав на запись в каталог конфигурации.</exception>
        public static void Save(AppConfig config)
        {
            Directory.CreateDirectory(ConfigDirectory);
            Normalize(config);
            File.WriteAllText(ConfigFilePath, JsonSerializer.Serialize(config, JsonOptions));
        }

        /// <summary>
        /// Создаёт конфигурацию по умолчанию: SQLite с файлом <c>data.db</c> рядом с конфигом.
        /// </summary>
        /// <returns>Новая конфигурация с предустановленными значениями.</returns>
        public static AppConfig CreateDefault() => new()
        {
            DatabaseType = DatabaseType.SQLite,
            Sqlite = new SqliteSettings
            {
                DatabasePath = Path.Combine(ConfigDirectory, "data.db")
            },
            MySql = new MySqlSettings
            {
                Server = "localhost",
                Port = 3306,
                Database = "adlp",
                User = "root",
                Password = ""
            }
        };

        /// <summary>
        /// Формирует строку подключения на основании конфигурации.
        /// </summary>
        /// <param name="cfg">Конфигурация приложения.</param>
        /// <returns>Готовая строка подключения для выбранного типа СУБД.</returns>
        /// <exception cref="NotSupportedException">Тип СУБД из конфигурации не поддержан.</exception>
        public static string BuildConnectionString(AppConfig cfg) => cfg.DatabaseType switch
        {
            DatabaseType.SQLite => $"Data Source={cfg.Sqlite.DatabasePath};",
            DatabaseType.MySQL => BuildMySqlConnectionString(cfg.MySql),
            _ => throw new NotSupportedException($"Тип БД {cfg.DatabaseType} не поддержан.")
        };

        /// <summary>
        /// Приводит конфигурацию к корректному состоянию: подставляет путь к БД по умолчанию, если он пуст.
        /// </summary>
        /// <param name="cfg">Конфигурация для нормализации (изменяется на месте).</param>
        private static void Normalize(AppConfig cfg)
        {
            if(string.IsNullOrWhiteSpace(cfg.Sqlite.DatabasePath))
                cfg.Sqlite.DatabasePath = Path.Combine(ConfigDirectory, "data.db");
        }

        private static string BuildMySqlConnectionString(MySqlSettings s)
        {
            var builder = new MySqlConnectionStringBuilder
            {
                Server = s.Server,
                Port = (uint)s.Port,
                Database = s.Database,
                UserID = s.User,
                Password = s.Password,
                AllowUserVariables = true,
                SslMode = MySqlSslMode.Preferred,
                AllowPublicKeyRetrieval = true
            };

            if(s.IntegratedSecurity)
            {
                builder.IntegratedSecurity = true;
            }

            return builder.ConnectionString;
        }
    }
}
