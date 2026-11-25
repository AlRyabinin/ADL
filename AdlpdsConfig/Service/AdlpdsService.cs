using AdlpLib.Backup;
using AdlpLib.Core;
using AdlpLib.Core.Modules.Types;
using AdlpLib.Core.Packet.Config;
using AdlpLib.Devices;
using AdlpLib.Fw;
using AdlpLib.LiveConfig;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace AdlpdsConfig.Service
{
    internal class AdlpdsService : IDisposable
    {
        private Configurator Cfg;

        /// <summary>
        /// Модуль бэкапа настроек устройств
        /// </summary>
        public BackupCfgModule Backup;

        /// <summary>
        /// Модуль конфигурации устройств наживую
        /// </summary>
        public LiveConfigModule Live;

        /// <summary>
        /// Каталог файлов бэкапов
        /// </summary>
        public static string BackupsDir => Path.Combine(AppContext.BaseDirectory, "backups");

        public sealed class MulticastSettings
        {
            public byte Transport { get; set; } = 0;

            public string OwnAddress { get; set; } = "";

            public List<string> Subscriptions { get; set; } = new();
        }

        public const int MaxIgmpSlots = 32;

        public AdlpdsService(Net N, int Block = 1)
        {
            Cfg = new Configurator(N, CreateGuid("CFG", 1));
            Backup = new BackupCfgModule();
            Cfg.AddModule(Backup);
            Live = new LiveConfigModule();
            Cfg.AddModule(Live);
            Cfg.SetBlock(Block);
            Cfg.Init();
        }

        static Guid CreateGuid(string Name, int Index)
        {
            var Raw = new byte[16] { 0, 0, 0, 0,
                                         0, 0, 0, 0,
                                         0, 0, 0, 0,
                                         0, 0, 0, 0};
            var Id = ASCIIEncoding.UTF8.GetBytes(Name);
            Array.Copy(Id, Raw, Id.Length);
            Raw[15] = Convert.ToByte(Index);

            return new Guid(Raw);
        }

        public void Dispose()
        {
            Cfg.Uninit();
        }

        public int OnlineDevices => Cfg.OnlineDevices;

        public Device[] GetDevices() => Cfg.GetDevices();

        public void ProbeBackup(Device device) => Backup.RequestProbe(device);

        public bool IsBackupBusy(Device device) => Backup.IsDeviceBusy(device);

        public BackedupDevice GetBackedupDevice(Device device) => Backup.GetBackedupDevice(device);

        public void ResetBackup(Device device) => Backup.ResetBackup(device);

        /// <summary>
        /// Запросить бэкап с устройства (DownloadData=false - только скан секций)
        /// </summary>
        public bool RequestBackup(Device device, bool downloadData) => Backup.RequestBackup(device, 0, downloadData);

        /// <summary>
        /// Сохранить бэкап устройства в файл (с фильтром секций)
        /// </summary>
        public bool SaveBackup(Device device, string fileName, IEnumerable<int> sectionIndices) => Backup.SaveToFile(device, fileName, sectionIndices);

        /// <summary>
        /// Загрузить бэкап на устройство из файла (с фильтром секций, игнорируя uuid)
        /// </summary>
        public bool UploadBackup(Device device, BackupFile file, IEnumerable<int> sectionIndices) => Backup.UploadBackup(device, file, true, sectionIndices);

        /// <summary>
        /// Прочитать файл бэкапа
        /// </summary>
        public BackupFile LoadBackupFile(string fileName) => Backup.LoadFromFile(fileName);

        /// <summary>
        /// Каталог файлов прошивок
        /// </summary>
        public static string FirmwareDir => Path.Combine(AppContext.BaseDirectory, "firmware");

        /// <summary>
        /// Прочитать файл прошивки .adlfw (с проверкой контрольных сумм)
        /// </summary>
        public AdlFirmwareFile LoadFirmwareFile(string fileName)
        {
            try
            {
                return AdlFirmwareFile.Load(fileName);
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Собрать пакет прошивки из YAML-спеки (.yaml/.yml)
        /// </summary>
        public AdlFirmwareFile BuildFirmwareFromSpec(string fileName)
        {
            try
            {
                var spec = AdlFirmwareSpec.Load(fileName, out var baseDir);
                return spec.Build(baseDir);
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Загрузить прошивку из файла: .adlfw — как есть, .yaml/.yml — собрать по спеке
        /// </summary>
        public AdlFirmwareFile LoadFirmwareAny(string fileName)
        {
            var ext = Path.GetExtension(fileName).ToLowerInvariant();
            return ext == ".yaml" || ext == ".yml"
                ? BuildFirmwareFromSpec(fileName)
                : LoadFirmwareFile(fileName);
        }

        /// <summary>
        /// Прошить устройство выбранной прошивкой
        /// </summary>
        public FirmwaredDevice FirmwareDevice(Device device, AdlFirmwareFile file)
        {
            if(device.Endpoint == null || device.Uuid == Guid.Empty)
                return null;
            return Cfg.Config.FirmwareDevice(device.Endpoint, device.Uuid, new AdlFirmware(file));
        }

        /// <summary>
        /// Определить параметры устройства (тип/версия ПО/производитель) командой DETECT
        /// </summary>
        public FirmwaredDevice ProbeFirmwareDevice(Device device) => Cfg.Config.ProbeFirmwareDevice(device);

        /// <summary>
        /// Текущее состояние прошивки устройства
        /// </summary>
        public FirmwaredDevice GetFirmwaredDevice(Device device) => Cfg.Config.GetFirmwaredDevice(device.Uuid);

        private static uint IpToU32(string s)
        {
            var raw = IPAddress.Parse(s).GetAddressBytes();
            return (uint)raw[0] << 24 | (uint)raw[1] << 16 | (uint)raw[2] << 8 | raw[3];
        }

        public void SetNet(Device device, string ip, string mask, string gateway)
        {
            var fields = new List<ParamDataField>
            {
                new ParamDataField(
                    new ParamDataId(ObjectCode.PARAM_OBJECT_DEVICE, 0, 0, ParamCode.DeviceNetIP),
                    new ParamDataValue(IpToU32(ip))),
                new ParamDataField(
                    new ParamDataId(ObjectCode.PARAM_OBJECT_DEVICE, 0, 0, ParamCode.DeviceNetMask),
                    new ParamDataValue(IpToU32(mask))),
            };
            if(gateway.Length > 0)
                fields.Add(new ParamDataField(
                    new ParamDataId(ObjectCode.PARAM_OBJECT_DEVICE, 0, 0, ParamCode.DeviceNetGateway),
                    new ParamDataValue(IpToU32(gateway))));

            Cfg.Config.SetParameters(device.Endpoint, fields.ToArray());
        }

        /// <summary>Идентичность устройства, считанная с самого устройства.</summary>
        public readonly record struct DeviceIdentity(uint SystemId, Guid Uuid, string Note);

        /// <summary>
        /// Прочитать SystemId устройства (в БД это SID: 8 hex-символов).
        /// </summary>
        public int GetSystemId(Device device, Action<uint, bool> onResult)
        {
            return Cfg.Config.GetParameters(
                device.Endpoint,
                new[]
                {
                    new ParamDataId(ObjectCode.PARAM_OBJECT_DEVICE, 0, 0, ParamCode.DeviceSystemId)
                },
                (fields, ok) =>
                {
                    if(!ok || fields == null || fields.Length == 0 ||
                       fields[0].Value == null ||
                       !TryGetU32Value(fields[0].Value, out uint systemId))
                    {
                        onResult?.Invoke(0, false);
                        return;
                    }

                    onResult?.Invoke(systemId, true);
                });
        }

        /// <summary>
        /// Прочитать UUID устройства (RAW[16], тот же порядок байт, что пишет SetUuid).
        /// </summary>
        public int GetDeviceUuid(Device device, Action<Guid, bool> onResult)
        {
            return Cfg.Config.GetParameters(
                device.Endpoint,
                new[]
                {
                    new ParamDataId(ObjectCode.PARAM_OBJECT_DEVICE, 0, 0, ParamCode.DeviceUUID)
                },
                (fields, ok) =>
                {
                    byte[] raw = ok && fields != null && fields.Length > 0
                        ? fields[0].Value?.Value
                        : null;

                    if(raw == null || raw.Length < 16)
                    {
                        onResult?.Invoke(Guid.Empty, false);
                        return;
                    }

                    onResult?.Invoke(new Guid(raw.AsSpan(0, 16)), true);
                });
        }

        /// <summary>
        /// Прочитать SystemId и UUID устройства. Если параметр прочитать не удалось,
        /// берутся значения из статуса устройства (их устройство рассылает само).
        /// Колбэк вызывается всегда. SystemId 0 допустим (SID 00000000).
        /// </summary>
        public int ReadIdentity(Device device, Action<DeviceIdentity, bool> onResult)
        {
            return GetSystemId(device, (systemId, okSid) =>
            {
                var notes = new List<string>();

                if(!okSid)
                {
                    systemId = device.SystemId;
                    notes.Add("SystemId не прочитан параметром, взят из статуса устройства.");
                }

                GetDeviceUuid(device, (uuid, okUuid) =>
                {
                    if(!okUuid || uuid == Guid.Empty)
                    {
                        uuid = device.Uuid;
                        notes.Add(uuid != Guid.Empty
                            ? "UUID взят из статуса устройства."
                            : "UUID на устройстве не задан или не прочитан.");
                    }

                    onResult?.Invoke(new DeviceIdentity(systemId, uuid, string.Join(" ", notes)), true);
                });
            });
        }

        public void SetSystemId(Device device, string sid)
        {
            if(!uint.TryParse(
                sid,
                System.Globalization.NumberStyles.HexNumber,
                null,
                out uint systemId))
            {
                throw new ArgumentException($"Некорректный SID: {sid}");
            }

            Cfg.Config.SetParameters(device.Endpoint, new[]
            {
        new ParamDataField(
            new ParamDataId(
                ObjectCode.PARAM_OBJECT_DEVICE,
                0, 0,
                ParamCode.DeviceSystemId),
            new ParamDataValue(systemId))
    });
        }

        public void SetUuid(Device device, string uuid)
        {
            if(!Guid.TryParse(uuid, out Guid guid))
                throw new ArgumentException($"Некорректный UUID: '{uuid}'", nameof(uuid));

            Cfg.Config.SetParameters(device.Endpoint, new ParamDataField[]
            {
                new ParamDataField(
                    new ParamDataId(ObjectCode.PARAM_OBJECT_DEVICE, 0, 0, ParamCode.DeviceUUID),
                    new ParamDataValue(ParamDataType.PARAM_DATA_TYPE_RAW, guid.ToByteArray()))
                    });
        }

        public void SetName(Device device, string name)
        {
            Cfg.Config.SetParameters(device.Endpoint, new ParamDataField[]
            {
                new ParamDataField(
                    new ParamDataId(ObjectCode.PARAM_OBJECT_DEVICE, 0, 0, ParamCode.DeviceName),
                    new ParamDataValue(name))
            });
        }

        public void SetMac(Device device, string mac)
        {
            var raw = Enumerable.Range(0, 6)
                .Select(i => Convert.ToByte(mac.Substring(i * 3, 2), 16))
                .ToArray();

            Cfg.Config.SetParameters(device.Endpoint,
            [
                new ParamDataField(
                    new ParamDataId(ObjectCode.PARAM_OBJECT_DEVICE, 0, 0, ParamCode.DeviceNetMAC),
                    new ParamDataValue(ParamDataType.PARAM_DATA_TYPE_RAW, raw))
            ]);
        }

        /// <summary>
        /// Прочитать MAC-адрес устройства.
        /// </summary>
        /// <param name="device">Устройство</param>
        /// <param name="onResult">Колбэк: (mac, ok). mac == null при ошибке/таймауте.</param>
        /// <returns>Номер транзакции или -1 при ошибке отправки</returns>
        public int GetMac(Device device, Action<string, bool> onResult)
        {
            return Cfg.Config.GetParameters(
                device.Endpoint,
                new[]
                {
                    new ParamDataId(ObjectCode.PARAM_OBJECT_DEVICE, 0, 0, ParamCode.DeviceNetMAC)
                    },
                    (fields, ok) =>
                    {
                        if(!ok || fields == null || fields.Length == 0)
                        {
                            onResult?.Invoke(null, false);
                            return;
                        }

                        var value = fields[0].Value;            
                        if(value == null)
                        {
                            onResult?.Invoke(null, false);
                            return;
                        }

                        byte[] raw = value.Value;               
                        if(raw == null || raw.Length < 6)
                        {
                            onResult?.Invoke(null, false);
                            return;
                        }

                        string mac = string.Join(":", raw.Take(6).Select(b => b.ToString("X2")));
                        onResult?.Invoke(mac, true);
                    });
        }

        /// <summary>
        /// Прочитать IP-адрес устройства.
        /// </summary>
        /// <param name="device">Устройство</param>
        /// <param name="onResult">Колбэк: (ip, ok). ip == null при ошибке/таймауте.</param>
        /// <returns>Номер транзакции или -1 при ошибке отправки</returns>
        public int GetIP(Device device, Action<string, bool> onResult)
        {
            return Cfg.Config.GetParameters(
                device.Endpoint,
                new[]
                {
                    new ParamDataId(ObjectCode.PARAM_OBJECT_DEVICE, 0, 0, ParamCode.DeviceNetIP)
                },
                (fields, ok) =>
                {
                    if(!ok || fields == null || fields.Length == 0)
                    {
                        onResult?.Invoke(null, false);
                        return;
                    }

                    var value = fields[0].Value;
                    if(value == null || value.DataType != ParamDataType.PARAM_DATA_TYPE_U32)
                    {
                        onResult?.Invoke(null, false);
                        return;
                    }

                    string ip = U32ToIp(value.GetU32Value());
                    onResult?.Invoke(ip, true);
                });
        }

        /// <summary>
        /// Прочитать маску подсети устройства.
        /// </summary>
        /// <param name="device">Устройство</param>
        /// <param name="onResult">Колбэк: (mask, ok). mask == null при ошибке/таймауте.</param>
        /// <returns>Номер транзакции или -1 при ошибке отправки</returns>
        public int GetMask(Device device, Action<string, bool> onResult)
        {
            return Cfg.Config.GetParameters(
                device.Endpoint,
                new[]
                {
                    new ParamDataId(ObjectCode.PARAM_OBJECT_DEVICE, 0, 0, ParamCode.DeviceNetMask)
                },
                (fields, ok) =>
                {
                    if(!ok || fields == null || fields.Length == 0)
                    {
                        onResult?.Invoke(null, false);
                        return;
                    }

                    var value = fields[0].Value;
                    if(value == null || value.DataType != ParamDataType.PARAM_DATA_TYPE_U32)
                    {
                        onResult?.Invoke(null, false);
                        return;
                    }

                    string mask = U32ToIp(value.GetU32Value());
                    onResult?.Invoke(mask, true);
                });
        }

        /// <summary>
        /// Прочитать шлюз устройства.
        /// </summary>
        /// <param name="device">Устройство</param>
        /// <param name="onResult">Колбэк: (gateway, ok). gateway == null при ошибке/таймауте.</param>
        /// <returns>Номер транзакции или -1 при ошибке отправки</returns>
        public int GetGateway(Device device, Action<string, bool> onResult)
        {
            return Cfg.Config.GetParameters(
                device.Endpoint,
                new[]
                {
            new ParamDataId(ObjectCode.PARAM_OBJECT_DEVICE, 0, 0, ParamCode.DeviceNetGateway)
                },
                (fields, ok) =>
                {
                    if(!ok || fields == null || fields.Length == 0)
                    {
                        onResult?.Invoke(null, false);
                        return;
                    }

                    var value = fields[0].Value;
                    if(value == null || value.DataType != ParamDataType.PARAM_DATA_TYPE_U32)
                    {
                        onResult?.Invoke(null, false);
                        return;
                    }

                    string gateway = U32ToIp(value.GetU32Value());
                    onResult?.Invoke(gateway, true);
                });
        }

        private static string U32ToIp(uint v)
        {
            return $"{(v >> 24) & 0xFF}.{(v >> 16) & 0xFF}.{(v >> 8) & 0xFF}.{v & 0xFF}";
        }

        /// <summary>
        /// Открыть базу конфигураций устройств (создастся при сохранении)
        /// </summary>
        public IDeviceConfigStore OpenConfigDatabase(string fileName) => new JsonDeviceConfigStore(fileName);

        /// <summary>
        /// Прочитать конфигурацию исполнительного устройства наживую.
        /// Колбэк выполняется в потоке ядра; результаты следует применять через BeginInvoke.
        /// </summary>
        public bool ReadExecConfig(Device device, int channelCount, int streamCount, Action<Device, LiveExecConfig, bool> done)
        {
            if(device.Endpoint == null)
                return false;

            var cfg = new LiveExecConfig { Block = device.Block };
            return Live.ReadParameters(device.Endpoint, LiveConfigCodec.BuildExecReadItems(channelCount, streamCount),
                (fields, ok) =>
                {
                    if(ok)
                        LiveConfigCodec.ApplyExecResponse(cfg, fields);
                    done(device, cfg, ok);
                });
        }

        /// <summary>
        /// Записать конфигурацию исполнительного устройства наживую.
        /// Колбэк выполняется в потоке ядра; результаты следует применять через BeginInvoke.
        /// </summary>
        public bool WriteExecConfig(Device device, LiveExecConfig config, Action<Device, bool> done)
        {
            if(device.Endpoint == null)
                return false;

            return Live.WriteParameters(device.Endpoint, LiveConfigCodec.BuildExecWriteFields(config),
                (fields, ok) => done(device, ok));
        }

        /// <summary>
        /// Прочитать конфигурацию управляющего устройства (ПУ) наживую.
        /// Цветной ПУ (тип «PU») читается с кнопками и настройками экрана.
        /// Колбэк выполняется в потоке ядра; результаты следует применять через BeginInvoke.
        /// </summary>
        public bool ReadControlConfig(Device device, Action<Device, LiveControlConfig, bool> done)
        {
            if(device.Endpoint == null)
                return false;

            bool color = LiveDeviceTypes.IsColorPanel(device.Name ?? "");
            var cfg = new LiveControlConfig { Block = device.Block > 0 ? device.Block : 1 };
            return Live.ReadParametersSequential(device.Endpoint,
                LiveConfigCodec.BuildControlReadItems(
                    LiveConfigDefaults.ControlChannels,
                    LiveConfigDefaults.ControlGroups,
                    LiveConfigDefaults.ControlPictures,
                    color),
                (fields, ok) =>
                {
                    if(ok)
                        LiveConfigCodec.ApplyControlResponse(cfg, fields);
                    done(device, cfg, ok);
                });
        }

        /// <summary>
        /// Записать конфигурацию управляющего устройства (ПУ) наживую.
        /// Колбэк выполняется в потоке ядра; результаты следует применять через BeginInvoke.
        /// </summary>
        public bool WriteControlConfig(Device device, LiveControlConfig config, Action<Device, bool> done)
        {
            if(device.Endpoint == null)
                return false;

            bool color = LiveDeviceTypes.IsColorPanel(device.Name ?? "");
            return Live.WriteParameters(device.Endpoint, LiveConfigCodec.BuildControlWriteFields(config, color),
                (fields, ok) => done(device, ok));
        }

        /// <summary>
        /// Прочитать конфигурацию гейта (каналы отображения линий управления) наживую.
        /// Колбэк выполняется в потоке ядра; результаты следует применять через BeginInvoke.
        /// </summary>
        public bool ReadGateConfig(Device device, Action<Device, LiveGateConfig, bool> done)
        {
            if(device.Endpoint == null)
                return false;

            var cfg = new LiveGateConfig { Block = device.Block > 0 ? device.Block : 1 };
            return Live.ReadParameters(device.Endpoint,
                LiveConfigCodec.BuildGateReadItems(LiveConfigDefaults.GateChannels),
                (fields, ok) =>
                {
                    if(ok)
                        LiveConfigCodec.ApplyGateResponse(cfg, fields);
                    done(device, cfg, ok);
                });
        }

        /// <summary>
        /// Записать конфигурацию гейта (каналы отображения линий управления) наживую.
        /// Колбэк выполняется в потоке ядра; результаты следует применять через BeginInvoke.
        /// </summary>
        public bool WriteGateConfig(Device device, LiveGateConfig config, Action<Device, bool> done)
        {
            if(device.Endpoint == null)
                return false;

            return Live.WriteParameters(device.Endpoint, LiveConfigCodec.BuildGateWriteFields(config),
                (fields, ok) => done(device, ok));
        }

        private static bool TryGetU32Value(
            ParamDataValue value,
            out uint result)
        {
            result = 0;

            const int requiredLength = ParamDataValue.Size + sizeof(uint);

            if(value.DataType != ParamDataType.PARAM_DATA_TYPE_U32 ||
                value.DataSize < requiredLength ||
                value.getLength() < requiredLength)
            {
                return false;
            }

            result = value.GetU32Value();
            return true;
        }

        private static bool TryGetU8Value(
            ParamDataValue value,
            out byte result)
        {
            result = 0;

            const int requiredLength = ParamDataValue.Size + sizeof(byte);

            if(value.DataType != ParamDataType.PARAM_DATA_TYPE_U8 ||
                value.DataSize < requiredLength ||
                value.getLength() < requiredLength)
            {
                return false;
            }

            result = (byte)value.GetU8Value();
            return true;
        }

        public bool ReadMulticast(Device device, Action<Device, MulticastSettings, bool> done)
        {
            var items = new List<GetParamItem>
            {
                new(new ParamDataId(ObjectCode.PARAM_OBJECT_DEVICE, 0, 0, ParamCode.DeviceStatusTransport), ParamDataType.PARAM_DATA_TYPE_U8),
                new(new ParamDataId(ObjectCode.PARAM_OBJECT_DEVICE, 0, 0, ParamCode.DeviceStatusMulticastAddress), ParamDataType.PARAM_DATA_TYPE_U32)
            };

            for(int i = 1; i <= MaxIgmpSlots; i++)
                items.Add(new GetParamItem(new ParamDataId(ObjectCode.PARAM_OBJECT_IGMP, 0, i, ParamCode.IgmpAddress), ParamDataType.PARAM_DATA_TYPE_U32));

            return Live.ReadParametersSequential(device.Endpoint, items.ToArray(), (fields, ok) =>
            {
                var result = new MulticastSettings();
                bool parseOk = ok && fields != null;

                try
                {
                    if(parseOk)
                    {
                        foreach(var field in fields!)
                        {
                            var value = field.Value;

                            if(field.Id.ObjectType == ObjectCode.PARAM_OBJECT_DEVICE)
                            {
                                if(field.Id.Param == ParamCode.DeviceStatusTransport)
                                {
                                    if(!TryGetU8Value(value, out byte transport))
                                    {
                                        System.Diagnostics.Debug.WriteLine("ReadMulticast: invalid Transport value");
                                        parseOk = false;
                                        break;
                                    }

                                    result.Transport = transport;
                                }
                                else if(field.Id.Param == ParamCode.DeviceStatusMulticastAddress)
                                {
                                    if(!TryGetU32Value(value, out uint address))
                                    {
                                        System.Diagnostics.Debug.WriteLine("ReadMulticast: invalid own address value");
                                        parseOk = false;
                                        break;
                                    }

                                    if(address != 0)
                                        result.OwnAddress = U32ToIp(address);
                                }
                            }
                            else if(field.Id.ObjectType == ObjectCode.PARAM_OBJECT_IGMP && field.Id.Param == ParamCode.IgmpAddress)
                            {
                                if(!TryGetU32Value(value, out uint address))
                                {
                                    // Пустой слот подписки не должен аварийно завершать чтение.
                                    System.Diagnostics.Debug.WriteLine(
                                            $"ReadMulticast: empty/invalid IGMP slot " +
                                            $"Index={field.Id.Index}, " +
                                            $"DataSize={value.DataSize}, " +
                                            $"BufferLength={value.getLength()}");

                                    continue;
                                }

                                if(address != 0)
                                    result.Subscriptions.Add(U32ToIp(address));
                            }
                        }
                    }
                }
                catch(Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"ReadMulticast parsing error: {ex}");

                    parseOk = false;
                }

                done?.Invoke(device, result, parseOk);
            });
        }

        public void WriteMulticast(Device device, MulticastSettings cfg)
        {
            var fields = new List<ParamDataField>
            {
                new(
                    new ParamDataId(
                        ObjectCode.PARAM_OBJECT_DEVICE, 0, 0,
                        ParamCode.DeviceStatusTransport),
                    new ParamDataValue(cfg.Transport)),

                new(
                    new ParamDataId(
                        ObjectCode.PARAM_OBJECT_DEVICE, 0, 0,
                        ParamCode.DeviceStatusMulticastAddress),
                    new ParamDataValue(
                        MulticastIpToU32(cfg.OwnAddress)))
            };

            for(int i = 1; i <= MaxIgmpSlots; i++)
            {
                string address = i <= cfg.Subscriptions.Count
                    ? cfg.Subscriptions[i - 1]
                    : "";

                fields.Add(new ParamDataField(
                    new ParamDataId(
                        ObjectCode.PARAM_OBJECT_IGMP, 0, i,
                        ParamCode.IgmpAddress),
                    new ParamDataValue(
                        MulticastIpToU32(address))));
            }

            Cfg.Config.SetParameters(device.Endpoint, fields.ToArray());
        }

        private static uint MulticastIpToU32(string s)
        {
            if(string.IsNullOrWhiteSpace(s) ||
                !IPAddress.TryParse(s.Trim(), out var ip))
                return 0;

            var raw = ip.GetAddressBytes();

            if(raw.Length != 4)
                return 0;

            return (uint)raw[0] << 24 |
                   (uint)raw[1] << 16 |
                   (uint)raw[2] << 8 |
                   raw[3];
        }
    }
}