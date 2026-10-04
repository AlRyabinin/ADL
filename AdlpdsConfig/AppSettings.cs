using System.Text;

namespace AdlpdsConfig
{
    /// <summary>
    /// Настройки приложения, хранящиеся в config.yaml в каталоге приложения
    /// </summary>
    internal static class AppSettings
    {
        public static string FilePath => Path.Combine(AppContext.BaseDirectory, "config.yaml");

        /// <summary>Путь к активной базе конфигураций</summary>
        public static string ActiveConfigDb { get; set; } = "";

        public static void Load()
        {
            ActiveConfigDb = "";
            try
            {
                if (!File.Exists(FilePath))
                    return;

                foreach (var raw in File.ReadAllLines(FilePath))
                {
                    var line = raw.Trim();
                    if (line.Length == 0 || line.StartsWith("#"))
                        continue;
                    int idx = line.IndexOf(':');
                    if (idx <= 0)
                        continue;
                    var key = line[..idx].Trim().Trim('"');
                    var value = line[(idx + 1)..].Trim().Trim('"');
                    if (key.Equals("config_db", StringComparison.OrdinalIgnoreCase))
                        ActiveConfigDb = value;
                }
            }
            catch
            {
            }
        }

        public static void Save()
        {
            try
            {
                File.WriteAllText(FilePath, $"config_db: {ActiveConfigDb}\n", Encoding.UTF8);
            }
            catch
            {
            }
        }
    }
}