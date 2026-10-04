using AdlpLib.LiveConfig;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace AdlpLib.Models.Devices.Exec
{
    public class ExecConfigDto : DeviceConfigDtoBase
    {
        [JsonPropertyOrder(10)]
        public string BaseChannel { get; set; }

        [JsonPropertyOrder(11)]
        public List<LiveChannelCfg> Channels { get; set; }

        [JsonPropertyOrder(12)]
        public List<LiveStreamCfg> Streams { get; set; }
    }
}
