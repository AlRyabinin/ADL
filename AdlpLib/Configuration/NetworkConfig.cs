using System;
using System.Collections.Generic;

namespace AdlpLib.Configuration
{
    /// <summary>
    /// Сетевые настройки интерфейса (файл network.conf)
    /// </summary>
    public class NetworkConfig
    {
        /// <summary>
        /// IP-адрес интерфейса (0.0.0.0 или auto — автоматический выбор)
        /// </summary>
        public string Ip { get; set; } = "0.0.0.0";

        /// <summary>
        /// Маска подсети
        /// </summary>
        public string Mask { get; set; } = "0.0.0.0";

        /// <summary>
        /// Произвольные дополнительные ключи файла
        /// </summary>
        public Dictionary<string, string> Params { get; } = new Dictionary<string, string>();

        public static NetworkConfig Load(string Path)
        {
            var Data = Yaml.Parse(Path);
            var Result = new NetworkConfig();

            foreach (var Item in Data)
            {
                switch (Item.Key.ToLower())
                {
                    case "ip": Result.Ip = Item.Value; break;
                    case "mask": Result.Mask = Item.Value; break;
                    default: Result.Params[Item.Key] = Item.Value; break;
                }
            }

            return Result;
        }

        /// <summary>
        /// Создать интерфейс для работы сокета
        /// </summary>
        public Network.NetworkInterface CreateInterface()
        {
            if (IsAutomatic(Ip))
                return Network.NetworkSelector.getDefaultInterface();

            var Mask = string.IsNullOrEmpty(this.Mask) ? "255.255.255.0" : this.Mask;

            return Network.NetworkSelector.getInterface(Ip, Mask);
        }

        private static bool IsAutomatic(string Ip)
        {
            if (string.IsNullOrEmpty(Ip))
                return true;

            return (Ip == "0.0.0.0") || Ip.Equals("auto", StringComparison.OrdinalIgnoreCase);
        }
    }
}