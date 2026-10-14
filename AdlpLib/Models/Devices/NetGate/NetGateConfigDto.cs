using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace AdlpLib.Models.Devices.NetGate
{
    /// <summary>
    /// DTO для сохранения и загрузки конфигурации устройства NetGate.
    /// </summary>
    public class NetGateConfigDto : DeviceConfigDtoBase
    {
        /// <summary>
        /// Количество активных конвейеров.
        /// </summary>
        [JsonPropertyOrder(10)]
        public int ConveyorCount { get; set; }

        /// <summary>
        /// Список конфигураций конвейеров.
        /// </summary>
        [JsonPropertyOrder(11)]
        public List<NetGateConveyorCfg> Conveyors { get; set; }

        /// <summary>
        /// Список каналов (линий).
        /// </summary>
        [JsonPropertyOrder(12)]
        public List<NetGateChannelCfg> Channels { get; set; }

        /// <summary>
        /// Список картинок (пресетов).
        /// </summary>
        [JsonPropertyOrder(13)]
        public List<NetGatePictureCfg> Pictures { get; set; }
    }
}
