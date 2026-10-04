using AdlpLib.Models.Devices.Exec;
using AdlpLib.Models.Devices.PU;
using System.Text.Json.Serialization;

namespace AdlpLib.Models.Devices
{
    [JsonPolymorphic(TypeDiscriminatorPropertyName = "configType")]
    [JsonDerivedType(typeof(PuConfigDto), "PU")]
    [JsonDerivedType(typeof(ExecConfigDto), "EXEC")]
    //дописывать новые
    public abstract class DeviceConfigDtoBase : IDeviceCommon
    {
        /// <summary>Идентификатор устройства в БД. Присваивается при сохранении.</summary>
        [System.Text.Json.Serialization.JsonIgnore]
        public long Id { get; set; }

        [JsonPropertyOrder(1)]
        public string Name { get; set; }

        [JsonPropertyOrder(2)]
        public string Mac { get; set; }

        [JsonPropertyOrder(3)]
        public string Ip { get; set; }

        [JsonPropertyOrder(4)]
        public string Mask { get; set; }

        [JsonPropertyOrder(5)]
        public string Gateway { get; set; }

        [JsonPropertyOrder(6)]
        public int Block { get; set; }
        
    }
}
