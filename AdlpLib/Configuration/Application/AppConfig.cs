using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AdlpLib.Configuration.Application
{

    /// <summary>
    /// Тип используемой СУБД.
    /// </summary>
    public enum DatabaseType
    {
        /// <summary>
        /// Локальная файловая база данных SQLite.
        /// </summary>
        SQLite,

        /// <summary>
        /// Серверная база данных MySQL.
        /// </summary>
        MySQL
        // Позже: SqlServer, PostgreSql, MySql...
    }

    /// <summary>
    /// Корневая модель конфигурации приложения.
    /// </summary>
    public class AppConfig
    {
        /// <summary>
        /// Тип активной СУБД. Определяет, какая секция настроек используется.
        /// </summary>
        public DatabaseType DatabaseType { get; set; } = DatabaseType.SQLite;

        /// <summary>
        /// Настройки подключения к SQLite.
        /// </summary>
        public SqliteSettings Sqlite { get; set; } = new();

        public MySqlSettings MySql { get; set; } = new();
    }

    /// <summary>
    /// Параметры подключения к SQLite.
    /// </summary>
    public class SqliteSettings
    {
        /// <summary>
        /// Полный путь к файлу базы данных. Если пусто — будет подставлен путь по умолчанию.
        /// </summary>
        public string DatabasePath { get; set; } = "";
    }

    /// <summary>
    /// Параметры подключения к MySQL.
    /// </summary>
    public class MySqlSettings
    {
        /// <summary>Адрес сервера (хост или IP).</summary>
        public string Server { get; set; } = "localhost";

        /// <summary>Порт сервера MySQL.</summary>
        public int Port { get; set; } = 3306;

        /// <summary>Имя базы данных.</summary>
        public string Database { get; set; } = "";

        /// <summary>Имя пользователя.</summary>
        public string User { get; set; } = "root";

        /// <summary>Пароль пользователя.</summary>
        public string Password { get; set; } = "";

        /// <summary>Использовать встроенную аутентификацию Windows.</summary>
        public bool IntegratedSecurity { get; set; } = false;
    }
}
