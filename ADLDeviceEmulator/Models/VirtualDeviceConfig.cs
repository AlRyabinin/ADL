using System.ComponentModel;
using System.Net;
using System.Text.Json.Serialization;

namespace ADLDeviceEmulator.Models;

public sealed class VirtualDeviceConfig
{
    public bool Enabled { get; set; } = true;
    public string Type { get; set; } = "IKB";
    public string DeviceName { get; set; } = "ADL006";
    public Guid Uuid { get; set; } = Guid.NewGuid();
    public int Block { get; set; } = 1;
    public int Port { get; set; } = 21001;
    public string IpAddress { get; set; } = "192.168.1.100";
    public string Mask { get; set; } = "255.255.255.0";
    public string Gateway { get; set; } = "192.168.1.1";
    public string Mac { get; set; } = "";

    // Legacy field kept for backward compatibility with previous devices.json files.
    // Once PartsModelInitialized is true, the typed collections are authoritative.
    public string ChannelIndexes { get; set; } = "100,101,102";
    public bool PartsModelInitialized { get; set; }

    public VirtualDeviceParameters Parameters { get; set; } = new();
    public List<EmulatorExecChannel> ExecChannels { get; set; } = new();
    public List<EmulatorStream> Streams { get; set; } = new();
    public List<EmulatorControlChannel> ControlChannels { get; set; } = new();
    public List<EmulatorGroup> Groups { get; set; } = new();
    public List<EmulatorPicture> Pictures { get; set; } = new();
    public List<EmulatorControlButton> Buttons { get; set; } = new();
    public List<EmulatorPixelgateChannel> PixelgateChannels { get; set; } = new();
    public List<EmulatorGateConveyor> GateConveyors { get; set; } = new();
    public List<EmulatorIgmpEntry> IgmpEntries { get; set; } = new();

    public byte Transport { get; set; }
    public string OwnAddress { get; set; } = "";
    public string Subscriptions { get; set; } = "";

    [JsonIgnore]
    public string Display => $"{Type} / {DeviceName} / блок {Block} / UDP {Port}";

    public VirtualDeviceConfig Clone() => new()
    {
        Enabled = Enabled,
        Type = Type,
        DeviceName = DeviceName,
        Uuid = Uuid,
        Block = Block,
        Port = Port,
        IpAddress = IpAddress,
        Mask = Mask,
        Gateway = Gateway,
        Mac = Mac,
        ChannelIndexes = ChannelIndexes,
        PartsModelInitialized = PartsModelInitialized,
        Parameters = Parameters?.Clone() ?? new VirtualDeviceParameters(),
        ExecChannels = (ExecChannels ?? new List<EmulatorExecChannel>()).Select(x => x.Clone()).ToList(),
        Streams = (Streams ?? new List<EmulatorStream>()).Select(x => x.Clone()).ToList(),
        ControlChannels = (ControlChannels ?? new List<EmulatorControlChannel>()).Select(x => x.Clone()).ToList(),
        Groups = (Groups ?? new List<EmulatorGroup>()).Select(x => x.Clone()).ToList(),
        Pictures = (Pictures ?? new List<EmulatorPicture>()).Select(x => x.Clone()).ToList(),
        Buttons = (Buttons ?? new List<EmulatorControlButton>()).Select(x => x.Clone()).ToList(),
        PixelgateChannels = (PixelgateChannels ?? new List<EmulatorPixelgateChannel>()).Select(x => x.Clone()).ToList(),
        GateConveyors = (GateConveyors ?? new List<EmulatorGateConveyor>()).Select(x => x.Clone()).ToList(),
        IgmpEntries = (IgmpEntries ?? new List<EmulatorIgmpEntry>()).Select(x => x.Clone()).ToList(),
        Transport = Transport,
        OwnAddress = OwnAddress,
        Subscriptions = Subscriptions
    };


    /// <summary>Копирует все настройки из другой конфигурации, создавая копии коллекций.</summary>
    public void CopyFrom(VirtualDeviceConfig source)
    {
        ArgumentNullException.ThrowIfNull(source);
        var clone = source.Clone();
        Enabled = clone.Enabled;
        Type = clone.Type;
        DeviceName = clone.DeviceName;
        Uuid = clone.Uuid;
        Block = clone.Block;
        Port = clone.Port;
        IpAddress = clone.IpAddress;
        Mask = clone.Mask;
        Gateway = clone.Gateway;
        Mac = clone.Mac;
        ChannelIndexes = clone.ChannelIndexes;
        PartsModelInitialized = clone.PartsModelInitialized;
        Parameters = clone.Parameters;
        ExecChannels = clone.ExecChannels;
        Streams = clone.Streams;
        ControlChannels = clone.ControlChannels;
        Groups = clone.Groups;
        Pictures = clone.Pictures;
        Buttons = clone.Buttons;
        PixelgateChannels = clone.PixelgateChannels;
        GateConveyors = clone.GateConveyors;
        IgmpEntries = clone.IgmpEntries;
        Transport = clone.Transport;
        OwnAddress = clone.OwnAddress;
        Subscriptions = clone.Subscriptions;
    }

    public void EnsurePartsInitialized()
    {
        ExecChannels ??= new List<EmulatorExecChannel>();
        Streams ??= new List<EmulatorStream>();
        ControlChannels ??= new List<EmulatorControlChannel>();
        Groups ??= new List<EmulatorGroup>();
        Pictures ??= new List<EmulatorPicture>();
        Buttons ??= new List<EmulatorControlButton>();
        PixelgateChannels ??= new List<EmulatorPixelgateChannel>();
        GateConveyors ??= new List<EmulatorGateConveyor>();
        IgmpEntries ??= new List<EmulatorIgmpEntry>();
        Parameters ??= new VirtualDeviceParameters();

        if(PartsModelInitialized)
            return;

        int[] legacy = ParseIndexes(ChannelIndexes);
        int baseIndex = Math.Max(1, 1 + (Math.Max(1, Block) - 1) * 24);
        string type = NormalizeType(Type);

        if(IsExecType(type) && ExecChannels.Count == 0)
        {
            // LiveConfigCodec запрашивает 24 локальных канала EXEC.
            for(int i = 0; i < 24; i++)
            {
                int globalIndex = i < legacy.Length ? legacy[i] : baseIndex + i;
                ExecChannels.Add(new EmulatorExecChannel
                {
                    Local = i + 1,
                    GlobalIndex = globalIndex,
                    Name = $"Канал {globalIndex}",
                    Control = "adlp",
                    StreamId = PackStreamId("device", 0),
                    Limit = 100
                });
            }
        }
        else if(IsControlType(type) && ControlChannels.Count == 0)
        {
            // В штатной конфигурации ПУ предусмотрено 48 каналов.
            for(int i = 0; i < 48; i++)
            {
                int globalIndex = i < legacy.Length ? legacy[i] : baseIndex + i;
                ControlChannels.Add(new EmulatorControlChannel
                {
                    Local = i + 1,
                    Name = $"Канал {globalIndex}",
                    Index = globalIndex,
                    Type = 1
                });
            }
        }
        else if(IsGateType(type) && PixelgateChannels.Count == 0)
        {
            // LiveConfigDefaults задаёт 20 линий PixelGate.
            for(int i = 0; i < 20; i++)
            {
                int globalIndex = i < legacy.Length ? legacy[i] : baseIndex + i;
                PixelgateChannels.Add(new EmulatorPixelgateChannel
                {
                    Local = i + 1,
                    Name = $"Линия {globalIndex}",
                    Index = globalIndex
                });
            }
        }

        // The control configuration protocol has fixed local object ranges.
        if(IsControlType(type))
        {
            if(Groups.Count == 0)
            {
                for(int i = 1; i <= 24; i++)
                    Groups.Add(new EmulatorGroup { Local = i, Index = i, Channels = "" });
            }
            if(Pictures.Count == 0)
            {
                for(int i = 1; i <= 24; i++)
                    Pictures.Add(new EmulatorPicture { Local = i, Name = "" });
            }
            if(string.Equals(type, "PU", StringComparison.OrdinalIgnoreCase) && Buttons.Count == 0)
            {
                for(int i = 1; i <= 96; i++)
                    Buttons.Add(new EmulatorControlButton { Local = i, Page = 1 });
            }
        }

        if(IgmpEntries.Count == 0)
        {
            var subscriptions = ParseSubscriptions(Subscriptions);
            for(int i = 0; i < subscriptions.Length; i++)
                IgmpEntries.Add(new EmulatorIgmpEntry { Slot = i + 1, Address = subscriptions[i] });
        }

        if(Parameters.StartChannel <= 0 || (Parameters.StartChannel == 1 && Block > 1))
            Parameters.StartChannel = baseIndex;

        SyncLegacyChannelIndexes();
        PartsModelInitialized = true;
    }

    public int[] GetChannelIndexes()
    {
        if(PartsModelInitialized)
        {
            string type = NormalizeType(Type);
            IEnumerable<int> indexes = IsExecType(type)
                ? ExecChannels.Select(x => x.GlobalIndex)
                : IsControlType(type)
                    ? ControlChannels.Where(x => x.Type == 1).Select(x => x.Index)
                    : IsGateType(type)
                        ? PixelgateChannels.Select(x => x.Index)
                        : Enumerable.Empty<int>();

            return indexes.Where(x => x > 0 && x <= ushort.MaxValue).Distinct().ToArray();
        }

        return ParseIndexes(ChannelIndexes);
    }

    public void ApplyChannelIndexes(string? value)
    {
        int[] indexes = ParseIndexes(value);
        string type = NormalizeType(Type);

        if(IsExecType(type))
        {
            var old = ExecChannels.ToDictionary(x => x.Local, x => x);
            ExecChannels = indexes.Select((index, position) =>
            {
                int local = position + 1;
                if(old.TryGetValue(local, out var existing))
                {
                    var updated = existing.Clone();
                    updated.GlobalIndex = index;
                    return updated;
                }
                return new EmulatorExecChannel
                {
                    Local = local,
                    GlobalIndex = index,
                    Name = $"Channel {local}",
                    Control = "adlp",
                    StreamId = PackStreamId("device", 0),
                    Limit = 100
                };
            }).ToList();
        }
        else if(IsControlType(type))
        {
            var old = ControlChannels.ToDictionary(x => x.Local, x => x);
            ControlChannels = indexes.Select((index, position) =>
            {
                int local = position + 1;
                if(old.TryGetValue(local, out var existing))
                {
                    var updated = existing.Clone();
                    updated.Index = index;
                    updated.Type = 1;
                    return updated;
                }
                return new EmulatorControlChannel { Local = local, Name = $"Control {local}", Index = index, Type = 1 };
            }).ToList();
        }
        else if(IsGateType(type))
        {
            var old = PixelgateChannels.ToDictionary(x => x.Local, x => x);
            PixelgateChannels = indexes.Select((index, position) =>
            {
                int local = position + 1;
                if(old.TryGetValue(local, out var existing))
                {
                    var updated = existing.Clone();
                    updated.Index = index;
                    return updated;
                }
                return new EmulatorPixelgateChannel { Local = local, Name = $"Line {local}", Index = index };
            }).ToList();
        }

        ChannelIndexes = string.Join(",", indexes);
        PartsModelInitialized = true;
    }

    public void SyncLegacyChannelIndexes()
    {
        string type = NormalizeType(Type);
        IEnumerable<int> indexes = IsExecType(type)
            ? ExecChannels.OrderBy(x => x.Local).Select(x => x.GlobalIndex)
            : IsControlType(type)
                ? ControlChannels.OrderBy(x => x.Local).Where(x => x.Type == 1).Select(x => x.Index)
                : IsGateType(type)
                    ? PixelgateChannels.OrderBy(x => x.Local).Select(x => x.Index)
                    : Enumerable.Empty<int>();

        ChannelIndexes = string.Join(",", indexes.Where(x => x > 0));
        PartsModelInitialized = true;
    }

    public string[] GetSubscriptionList()
    {
        if(PartsModelInitialized)
        {
            return (IgmpEntries ?? new List<EmulatorIgmpEntry>())
                .Where(x => !string.IsNullOrWhiteSpace(x.Address))
                .OrderBy(x => x.Slot)
                .Select(x => x.Address.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Take(32)
                .ToArray();
        }

        // Только старые JSON без typed-модели читают legacy-строку Subscriptions.
        return ParseSubscriptions(Subscriptions);
    }

    public void SetSubscriptionList(IEnumerable<string> values)
    {
        string[] distinct = values
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(32)
            .ToArray();
        Subscriptions = string.Join(";", distinct);
        IgmpEntries = distinct.Select((address, index) => new EmulatorIgmpEntry
        {
            Slot = index + 1,
            Address = address
        }).ToList();
        PartsModelInitialized = true;
    }

    public static bool IsExecType(string type) => type is "IKB" or "EXEC" or "DIM220" or "DIMCABINET";
    public static bool IsControlType(string type) => type is "PU" or "PU24" or "PU6" or "PUBW";
    public static bool IsGateType(string type) => type is "INDIGATE" or "NETGATE";

    public static string NormalizeType(string? type) => (type ?? string.Empty).Trim().Replace(" ", "").ToUpperInvariant();

    private static int[] ParseIndexes(string? value) => (value ?? string.Empty)
        .Split(new[] { ',', ';', ' ', '\t', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
        .Select(x => int.TryParse(x, out int parsed) ? parsed : 0)
        .Where(x => x > 0 && x <= ushort.MaxValue)
        .Distinct()
        .ToArray();

    private static string[] ParseSubscriptions(string? value) => (value ?? string.Empty)
        .Split(new[] { ',', ';', ' ', '\t', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
        .Select(x => x.Trim())
        .Where(x => IPAddress.TryParse(x, out var ip) && ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .Take(32)
        .ToArray();

    public static uint PackStreamId(string? protocol, int universe)
    {
        uint type = (protocol ?? "").Trim().ToLowerInvariant() switch
        {
            "dmx" => 1,
            "artnet" => 2,
            "acn" or "sacn" => 3,
            "device" => 4,
            _ => 0
        };
        return ((type & 0xFF) << 16) | ((uint)universe & 0xFFFF);
    }

    public static string StreamProtocolName(uint packed) => ((packed >> 16) & 0xFF) switch
    {
        0 => "нет",
        1 => "dmx",
        2 => "artnet",
        3 => "sACN",
        4 => "device",
        _ => "device"
    };

    public static List<VirtualDeviceConfig> CreateDemoSet(string ipAddress)
    {
        return new List<VirtualDeviceConfig>
        {
            new()
            {
                Type = "IKB", DeviceName = "ADL006 IKB", Block = 1, Port = 21001,
                IpAddress = ipAddress, ChannelIndexes = "100,101,102,103",
                Mac = "02:AD:00:00:00:01", OwnAddress = "239.251.1.0"
            },
            new()
            {
                Type = "PU", DeviceName = "ADL006 COLOR", Block = 1, Port = 21002,
                IpAddress = ipAddress, ChannelIndexes = "100,200,201",
                Mac = "02:AD:00:00:00:02", OwnAddress = "239.251.11.0"
            },
            new()
            {
                Type = "PU24", DeviceName = "ADL006 BW", Block = 1, Port = 21003,
                IpAddress = ipAddress, ChannelIndexes = "102,300,301",
                Mac = "", OwnAddress = "239.251.12.0"
            },
            new()
            {
                Type = "INDIGATE", DeviceName = "ADL INDIGATE", Block = 1, Port = 21004,
                IpAddress = ipAddress, ChannelIndexes = "200,400,401",
                Mac = "02:AD:00:00:00:04", OwnAddress = "239.251.21.0"
            }
        };
    }
}

public sealed class VirtualDeviceParameters
{
    [Category("Основные")]
    public int Priority { get; set; } = 5;
    [Category("Основные")]
    public int StartChannel { get; set; } = 1;
    [Category("Основные")]
    public int PictureSwitchTime { get; set; }
    [Category("Основные")]
    public uint PictureStream { get; set; }
    [Category("Основные")]
    public int PictureFrameTime { get; set; }
    [Category("Основные")]
    public uint SystemId { get; set; }
    [Category("Основные")]
    public int TimeRequest { get; set; }
    [Category("Основные")]
    public string Location { get; set; } = "";
    [Category("Экран ПУ")]
    public int PageCount { get; set; } = 1;
    [Category("Экран ПУ")]
    public int TouchscreenSwapX { get; set; }
    [Category("Экран ПУ")]
    public int TouchscreenSwapY { get; set; }
    [Category("Экран ПУ")]
    public int Brightness { get; set; } = 100;
    [Category("Экран ПУ")]
    public int BrightnessSleep { get; set; } = 100;
    [Category("Экран ПУ")]
    public int SleepTimeout { get; set; }
    [Category("Сеть Art-Net")]
    public bool ArtnetNet2Enabled { get; set; }
    [Category("Сеть Art-Net")]
    public bool ArtnetNet10Enabled { get; set; }
    [Category("Сеть Art-Net")]
    public string ArtnetNet2Address { get; set; } = "0.0.0.0";
    [Category("Сеть Art-Net")]
    public string ArtnetNet2Netmask { get; set; } = "255.255.255.0";
    [Category("Сеть Art-Net")]
    public string ArtnetNet10Address { get; set; } = "0.0.0.0";
    [Category("Сеть Art-Net")]
    public string ArtnetNet10Netmask { get; set; } = "255.255.255.0";
    [Category("Прочее")]
    public int AcnVersion { get; set; }
    [Category("Прочее")]
    public int TargetTemperature { get; set; }

    public VirtualDeviceParameters Clone() => (VirtualDeviceParameters)MemberwiseClone();
}

public interface ILocalPart
{
    int Local { get; set; }
}

public sealed class EmulatorExecChannel : ILocalPart
{
    public int Local { get; set; }
    [DisplayName("Глобальный индекс")]
    public int GlobalIndex { get; set; }
    public string Name { get; set; } = "";
    [DisplayName("Управление (adlp/dmx)")]
    public string Control { get; set; } = "adlp";
    [DisplayName("Packed StreamId")]
    public uint StreamId { get; set; }
    public int Patch { get; set; }
    public bool Wide { get; set; }
    public int Preheat { get; set; }
    public int Limit { get; set; } = 100;
    public int Curve { get; set; }
    public int SwitchDelay { get; set; }
    public EmulatorExecChannel Clone() => (EmulatorExecChannel)MemberwiseClone();
}

public sealed class EmulatorStream : ILocalPart
{
    [Browsable(false)]
    public int Local { get => Index; set => Index = value; }
    public int Index { get; set; }
    public string Protocol { get; set; } = "нет";
    public int Universe { get; set; }
    public int Address { get; set; } = 1;
    [Browsable(false)]
    public uint PackedId => VirtualDeviceConfig.PackStreamId(Protocol, Universe);
    public EmulatorStream Clone() => (EmulatorStream)MemberwiseClone();
}

public sealed class EmulatorControlChannel : ILocalPart
{
    public int Local { get; set; }
    public string Name { get; set; } = "";
    public int Index { get; set; }
    [DisplayName("Тип (0=нет,1=канал,2=группа,3=картина)")]
    public int Type { get; set; } = 1;
    public EmulatorControlChannel Clone() => (EmulatorControlChannel)MemberwiseClone();
}

public sealed class EmulatorGroup : ILocalPart
{
    public int Local { get; set; }
    public int Index { get; set; }
    [DisplayName("Индексы каналов через запятую (до 100)")]
    public string Channels { get; set; } = "";
    public int[] GetChannels() => (Channels ?? "")
        .Split(new[] { ',', ';', ' ', '\t', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
        .Select(x => int.TryParse(x, out int n) ? n : 0)
        .Where(x => x >= 0 && x <= ushort.MaxValue)
        .Take(100)
        .ToArray();
    public EmulatorGroup Clone() => (EmulatorGroup)MemberwiseClone();
}

public sealed class EmulatorPicture : ILocalPart
{
    public int Local { get; set; }
    public string Name { get; set; } = "";
    public int TriggerPresetWrite { get; set; }
    public int FadeIn { get; set; }
    public int FadeOut { get; set; }
    public int Preset { get; set; }
    public int Mux1 { get; set; }
    public int Mux2 { get; set; }
    public int Mux3 { get; set; }
    public int Mux4 { get; set; }
    public EmulatorPicture Clone() => (EmulatorPicture)MemberwiseClone();
}

public sealed class EmulatorControlButton : ILocalPart
{
    public int Local { get; set; }
    public int Page { get; set; } = 1;
    public int LocalChannel { get; set; }
    public int PosX { get; set; }
    public int PosY { get; set; }
    public int Icon { get; set; }
    public int Color { get; set; }
    public EmulatorControlButton Clone() => (EmulatorControlButton)MemberwiseClone();
}

public sealed class EmulatorPixelgateChannel : ILocalPart
{
    public int Local { get; set; }
    public string Name { get; set; } = "";
    public int Index { get; set; }
    public EmulatorPixelgateChannel Clone() => (EmulatorPixelgateChannel)MemberwiseClone();
}

public sealed class EmulatorGateConveyor : ILocalPart
{
    public int Local { get; set; }
    public bool Recordable { get; set; }
    [DisplayName("Source A (U32)")]
    public uint SourceA { get; set; }
    [DisplayName("Source B (U32)")]
    public uint SourceB { get; set; }
    [DisplayName("Source C (U32)")]
    public uint SourceC { get; set; }
    [DisplayName("Destination (U32)")]
    public uint Destination { get; set; }
    public int SourceSelect { get; set; }
    public int SumType { get; set; }
    public int SumBFrom { get; set; }
    public int SumBTo { get; set; }
    public int SumCFrom { get; set; }
    public int SumCTo { get; set; }
    [DisplayName("Frequency (локально, не кодируется)")]
    public int Frequency { get; set; }
    [DisplayName("Timeouted (локально, не кодируется)")]
    public bool Timeouted { get; set; }
    [DisplayName("RdmEnabled (локально, не кодируется)")]
    public bool RdmEnabled { get; set; }
    public EmulatorGateConveyor Clone() => (EmulatorGateConveyor)MemberwiseClone();
}

public sealed class EmulatorIgmpEntry : ILocalPart
{
    [Browsable(false)]
    public int Local { get => Slot; set => Slot = value; }
    public int Slot { get; set; }
    public string Address { get; set; } = "";
    public EmulatorIgmpEntry Clone() => (EmulatorIgmpEntry)MemberwiseClone();
}
