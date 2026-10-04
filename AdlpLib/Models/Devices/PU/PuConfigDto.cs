using AdlpLib.LiveConfig;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace AdlpLib.Models.Devices.PU
{
    public class PuConfigDto : DeviceConfigDtoBase
    {
        /// <summary>Приоритет устройства</summary>
        [JsonPropertyOrder(7)]
        public int Priority { get; set; } = 5;

        /// <summary>Время перехода между картинами, мс</summary>
        [JsonPropertyOrder(8)]
        public int PictureSwitchTime { get; set; }

        /// <summary>Число страниц кнопок (цветной ПУ)</summary>
        [JsonPropertyOrder(9)]
        public int PageCount { get; set; } = 1;

        /// <summary>Поменять ось X сенсора (цветной ПУ)</summary>
        [JsonPropertyOrder(10)]
        public int SwapX { get; set; }

        /// <summary>Поменять ось Y сенсора (цветной ПУ)</summary>
        [JsonPropertyOrder(11)]
        public int SwapY { get; set; }

        /// <summary>Яркость, % (цветной ПУ)</summary>
        [JsonPropertyOrder(12)]
        public int Brightness { get; set; } = 100;

        /// <summary>Яркость при сне, % (цветной ПУ)</summary>
        [JsonPropertyOrder(13)]
        public int BrightnessSleep { get; set; } = 100;

        /// <summary>Время до сна, с (цветной ПУ)</summary>
        [JsonPropertyOrder(14)]
        public int SleepTimeout { get; set; }

        [JsonPropertyOrder(15)]
        public List<LiveControlChannelCfg> Channels { get; set; }

        [JsonPropertyOrder(16)]
        /// <summary>Группы</summary>
        public List<LiveControlGroupCfg> Groups { get; set; } = new List<LiveControlGroupCfg>();

        /// <summary>Картины</summary>
        [JsonPropertyOrder(17)]
        public List<LiveControlPictureCfg> Pictures { get; set; } = new List<LiveControlPictureCfg>();

        /// <summary>Кнопки цветного ПУ</summary>
        [JsonPropertyOrder(18)]
        public List<LiveControlButtonCfg> Buttons { get; set; } = new List<LiveControlButtonCfg>();
    }
}
