using AdlpLib.Models.Devices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace AdlpLib.Models.DevicesGroup
{
    /// <summary>Группа устройств (PU + EXEC).</summary>
    public class DevicesGroupDto
    {
        /// <summary>Идентификатор группы в БД. Присваивается при сохранении.</summary>
        [JsonIgnore]
        public long Id { get; set; }

        /// <summary>Название группы.</summary>
        [JsonPropertyOrder(0)]
        public string Name { get; set; }

        /// <summary>Идентификаторы устройств, входящих в группу.</summary>
        [JsonPropertyOrder(1)]
        public List<long> DeviceIds { get; set; } = new();
    }
}
