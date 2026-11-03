using AdlpLib.Models.Devices.Exec;
using AdlpLib.Models.Devices.NetGate;
using AdlpLib.Models.Devices.PU;
using System;
using System.Text.Json.Serialization;

namespace AdlpLib.Models.Devices
{
    [JsonPolymorphic(TypeDiscriminatorPropertyName = "configType")]
    [JsonDerivedType(typeof(Pu6ConfigDto), "PU 6")]
    [JsonDerivedType(typeof(PuBWConfigDto), "PU black-and-white")]
    [JsonDerivedType(typeof(PuColorConfigDto), "PU color")]
    [JsonDerivedType(typeof(IkbConfigDto), "IKB")]
    [JsonDerivedType(typeof(Dimmer220ConfigDto), "DIM220")]
    [JsonDerivedType(typeof(DimmerCabinetConfigDto), "DIMCABINET")]
    [JsonDerivedType(typeof(ExecConfigDto), "EXEC")]
    [JsonDerivedType(typeof(NetGateConfigDto), "NETGATE")]
    public abstract class DeviceConfigDtoBase : IDeviceCommon
    {
        /// <summary>Идентификатор устройства в БД. Присваивается при сохранении.</summary>
        [JsonIgnore]
        public long Id { get; set; }

        [JsonPropertyOrder(1)]
        public string Name { get; set; }

        [JsonIgnore]
        public string DeviceType => GetType().Name switch
        {
            nameof(Pu6ConfigDto) => "Pu 6",
            nameof(PuBWConfigDto) => "Pu black-and-white",
            nameof(PuColorConfigDto) => "Pu color",
            nameof(IkbConfigDto) => "IKB",
            nameof(Dimmer220ConfigDto) => "DIM220",
            nameof(DimmerCabinetConfigDto) => "Dimmer cabinet",
            nameof(ExecConfigDto) => "IKB",
            nameof(NetGateConfigDto) => "NetGate",
            _ => "unknown"
        };

        [JsonPropertyOrder(2)]
        public int SID { get; set; }

        [JsonPropertyOrder(3)]
        public string Mac { get; set; }

        [JsonPropertyOrder(4)]
        public string Ip { get; set; }

        [JsonPropertyOrder(5)]
        public string Mask { get; set; }

        [JsonPropertyOrder(6)]
        public string Gateway { get; set; }

        [JsonPropertyOrder(7)]
        public int Block { get; set; }

        [JsonPropertyOrder(8)]
        public Guid? Uuid { get; set; }
    }
}
