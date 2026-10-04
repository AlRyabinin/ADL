using System;
using System.Collections.Generic;
using System.IO;

namespace AdlpLib.Configuration
{
    /// <summary>
    /// Набор конфигураций одного запуска: сетевой интерфейс + устройства на нём.
    /// Автодетект: первый файл, содержащий ключи ip/mask, считается сетевой конфигурацией,
    /// остальные файлы — конфигурациями устройств. Если сетевой конфиг не задан,
    /// берётся network.conf (или network.yaml) из каталога приложения.
    /// </summary>
    public class Configurations
    {
        /// <summary>
        /// Имя сетевого конфига по умолчанию
        /// </summary>
        public const string DefaultConfigName = "network.conf";

        /// <summary>
        /// Сетевая конфигурация интерфейса
        /// </summary>
        public NetworkConfig Network { get; set; } = new NetworkConfig();

        /// <summary>
        /// Конфигурации устройств
        /// </summary>
        public List<DeviceConfig> Devices { get; } = new List<DeviceConfig>();

        /// <summary>
        /// Путь к сетевому конфигу по умолчанию (каталог приложения)
        /// </summary>
        public static string GetDefaultNetworkPath()
        {
            return Path.Combine(AppContext.BaseDirectory, DefaultConfigName);
        }

        /// <summary>
        /// Загрузить конфигурации из аргументов командной строки.
        /// </summary>
        /// <param name="Args">аргументы после команды</param>
        /// <param name="Consumed">сколько ведущих аргументов оказалось файлами конфигураций</param>
        public static Configurations Load(string[] Args, out int Consumed)
        {
            var Result = new Configurations();
            Consumed = 0;

            if (Args != null)
            {
                while ((Consumed < Args.Length) && File.Exists(Args[Consumed]))
                    Consumed++;
            }

            var Files = new List<string>();
            for (int i = 0; i < Consumed; i++)
                Files.Add(Args[i]);

            var NetworkLoaded = false;
            foreach (var File in Files)
            {
                if (!NetworkLoaded && IsNetworkFile(File))
                {
                    Result.Network = NetworkConfig.Load(File);
                    NetworkLoaded = true;
                }
                else
                {
                    Result.Devices.Add(DeviceConfig.Load(File));
                }
            }

            if (!NetworkLoaded)
            {
                var Default = FindDefaultNetworkFile();
                if (!File.Exists(Default))
                    throw new FileNotFoundException(
                        $"Сетевой конфиг 'network.conf'/'network.yaml' не найден в каталоге приложения: {AppContext.BaseDirectory}. Укажите путь к нему явно или создайте файл рядом с приложением.");

                Result.Network = NetworkConfig.Load(Default);
            }

            return Result;
        }

        /// <summary>
        /// Сетевой ли это конфиг (содержит ключи ip/mask)
        /// </summary>
        private static bool IsNetworkFile(string Path)
        {
            var Data = Yaml.Parse(Path);

            foreach (var Key in Data.Keys)
            {
                if (Key.Equals("ip", StringComparison.OrdinalIgnoreCase) ||
                    Key.Equals("mask", StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Найти сетевой конфиг по умолчанию в каталоге приложения
        /// </summary>
        private static string FindDefaultNetworkFile()
        {
            foreach (var Name in new[] { DefaultConfigName, "network.yaml" })
            {
                var Path = System.IO.Path.Combine(AppContext.BaseDirectory, Name);
                if (File.Exists(Path))
                    return Path;
            }

            return GetDefaultNetworkPath();
        }

        /// <summary>
        /// Создать сокетный интерфейс из сетевой конфигурации
        /// </summary>
        public AdlpLib.Core.Net CreateNet()
        {
            return new AdlpLib.Core.Net(Network.CreateInterface());
        }
    }
}