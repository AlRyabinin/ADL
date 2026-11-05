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
    public string ChannelIndexes { get; set; } = "100,101,102";
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
        Transport = Transport,
        OwnAddress = OwnAddress,
        Subscriptions = Subscriptions
    };

    public int[] GetChannelIndexes()
    {
        return (ChannelIndexes ?? string.Empty)
            .Split(new[] { ',', ';', ' ', '\t', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(x => int.TryParse(x, out int value) ? value : 0)
            .Where(x => x > 0 && x <= ushort.MaxValue)
            .Distinct()
            .ToArray();
    }

    public string[] GetSubscriptionList()
    {
        return (Subscriptions ?? string.Empty)
            .Split(new[] { ',', ';', ' ', '\t', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(x => x.Trim())
            .Where(x => IPAddress.TryParse(x, out var ip) && ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(32)
            .ToArray();
    }

    public void SetSubscriptionList(IEnumerable<string> values)
    {
        Subscriptions = string.Join(";", values.Where(x => !string.IsNullOrWhiteSpace(x)));
    }

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
