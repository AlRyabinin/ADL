using AdlpLib.Models.Devices;
using AdlpLib.Models.Devices.Exec;
using AdlpLib.Models.Devices.NetGate;
using AdlpLib.Models.Devices.PU;
using System;
using System.Collections.Generic;
using System.Linq;

namespace AdlpdsConfig.Controls.DevicesGroup
{
    /// <summary>Строка ComboBox: подпись + сам DTO.</summary>
    internal sealed class DeviceRow
    {
        public DeviceConfigDtoBase Device { get; }
        public string DisplayText { get; }

        public DeviceRow(DeviceConfigDtoBase device)
        {
            Device = device;

            string type = string.IsNullOrWhiteSpace(device.DeviceType) ? "?" : device.DeviceType;
            string name = string.IsNullOrWhiteSpace(device.Name) ? "(без имени)" : device.Name;
            string mac = string.IsNullOrWhiteSpace(device.Mac) ? "" : $" ({device.Mac})";
            string range = GetChannelRange(device);

            DisplayText = device switch
            {
                PuColorConfigDto => $"[{type}] {name}{mac} [{device.Block}] {range}",
                ExecConfigDto => $"[{type}] {name}{mac} [{device.Block}] {range}",
                _ => $"[{type}] {name}{mac} [{device.Block}] {range}"
            };
        }

        /// <summary>
        /// Возвращает строку вида "каналы 1-12" (или "канал 5" для одного канала).
        /// Если каналов нет — "-".
        /// </summary>
        private static string GetChannelRange(DeviceConfigDtoBase device)
        {
            List<int> indices;

            switch(device)
            {
                case PuColorConfigDto pu:
                    indices = pu.Channels?
                        .Select(c => c.Index)
                        .Where(i => i > 0)
                        .ToList() ?? new List<int>();
                    break;

                case ExecConfigDto exec:
                    indices = exec.Channels?
                        .Select(c => c.GlobalIndex)
                        .Where(i => i > 0)
                        .ToList() ?? new List<int>();
                    break;

                case NetGateConfigDto ng:
                    indices = ng.Channels?
                        .Select(c => c.Index)
                        .Where(i => i > 0)
                        .ToList() ?? new List<int>();
                    break;

                default:
                    return "-";
            }

            if(indices.Count == 0)
                return "-";

            int min = indices.Min();
            int max = indices.Max();

            if(min == max)
                return $"Канал {min}";

            return $"Каналы {min}-{max}";
        }

        public override string ToString() => DisplayText;
    }
}