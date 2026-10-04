using System;
using System.Collections.Generic;
using System.Text;

namespace AdlpLib.Configuration
{
    /// <summary>
    /// Настройки устройства (configurator.conf, ikb1.conf, pu1.conf, ...)
    /// </summary>
    public class DeviceConfig
    {
        /// <summary>
        /// Тип устройства: CFG, IKB, PU24, PROXY
        /// </summary>
        public string Type { get; set; } = "CFG";

        /// <summary>
        /// Номер блока
        /// </summary>
        public int Block { get; set; } = 1;

        /// <summary>
        /// Uuid; если пустой — генерируется из типа и номера блока
        /// </summary>
        public Guid Uuid { get; set; } = Guid.Empty;

        /// <summary>
        /// Каталог с JSON-конфигурацией устройства (ключ config)
        /// </summary>
        public string? ConfigPath { get; set; }

        /// <summary>
        /// Исходный каталог devices/groups для генерации конфигурации (ключ devconf)
        /// </summary>
        public string? DevConfigPath { get; set; }

        /// <summary>
        /// Каталог с прошивками устройства (ключ firmware)
        /// </summary>
        public string? FirmwarePath { get; set; }

        /// <summary>
        /// Произвольные дополнительные ключи файла
        /// </summary>
        public Dictionary<string, string> Params { get; } = new Dictionary<string, string>();

        public static DeviceConfig Load(string Path, string DefaultType = "CFG")
        {
            var Data = Yaml.Parse(Path);
            var Result = new DeviceConfig { Type = DefaultType };

            foreach (var Item in Data)
            {
                switch (Item.Key.ToLower())
                {
                    case "type":
                        Result.Type = Item.Value.ToUpper();
                        break;
                    case "block":
                        if (!int.TryParse(Item.Value, out var Block))
                            throw new FormatException($"{Path}: неверный block '{Item.Value}'");
                        Result.Block = Block;
                        break;
                    case "uuid":
                        if (!Guid.TryParse(Item.Value, out var Uuid))
                            throw new FormatException($"{Path}: неверный uuid '{Item.Value}'");
                        Result.Uuid = Uuid;
                        break;
                    case "config":
                        Result.ConfigPath = Resolve(Path, Item.Value);
                        break;
                    case "devconf":
                        Result.DevConfigPath = Resolve(Path, Item.Value);
                        break;
                    case "firmware":
                        Result.FirmwarePath = Resolve(Path, Item.Value);
                        break;
                    default:
                        Result.Params[Item.Key] = Item.Value;
                        break;
                }
            }

            return Result;
        }

        /// <summary>
        /// Uuid или авто-генерация из типа и номера блока
        /// </summary>
        public Guid GetUuid()
        {
            return (Uuid != Guid.Empty) ? Uuid : CreateGuid(Type, Block);
        }

        /// <summary>
        /// Относительные пути превращаются в пути относительно каталога конфига
        /// </summary>
        private static string Resolve(string ConfigFile, string Value)
        {
            if (string.IsNullOrEmpty(Value) || System.IO.Path.IsPathRooted(Value))
                return Value;

            var Dir = System.IO.Path.GetDirectoryName(ConfigFile);
            if (string.IsNullOrEmpty(Dir))
                return Value;

            return System.IO.Path.Combine(Dir, Value);
        }

        /// <summary>
        /// Генерация uuid из имени типа и индекса блока
        /// </summary>
        public static Guid CreateGuid(string Name, int Index)
        {
            var Raw = new byte[16];
            var Id = Encoding.UTF8.GetBytes(Name);
            Array.Copy(Id, Raw, Math.Min(Id.Length, Raw.Length));
            Raw[15] = Convert.ToByte(Index);

            return new Guid(Raw);
        }
    }
}