using AdlpLib.Configuration.Application;

namespace AdlpdsConfig
{
    internal static class Program
    {
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            ApplicationConfiguration.Initialize();

            if(!File.Exists(AppConfigService.ConfigFilePath))
            {
                MessageBox.Show(
                    "Конфигурация не найдена.\n\n" +
                    "Запустите приложение-конфигуратор AdlpDBConfigurator, " +
                    "чтобы настроить подключение к базе данных, после чего запустите " +
                    "это приложение снова.",
                    "Требуется настройка",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);

                return;
            }

            var cfg = AppConfigService.Load();
            var connectionString = AppConfigService.BuildConnectionString(cfg);

            // To customize application configuration such as set high DPI settings or default font,
            // see https://aka.ms/applicationconfiguration.
            ApplicationConfiguration.Initialize();
            Application.Run(new fMain());
        }
    }
}