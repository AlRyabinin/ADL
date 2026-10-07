using AdlpLib.Models.Devices;
using AdlpLib.Models.Devices.Exec;
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
                PuConfigDto => $"[PU]   [{type}] [{device.Block}] {name}{mac} {range}",
                ExecConfigDto => $"[EXEC] [{type}] [{device.Block}] {name}{mac} {range}",
                _ => $"[?]    [{type}] [{device.Block}] {name}{mac} {range}"
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
                case PuConfigDto pu:
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

                default:
                    return "-";
            }

            if(indices.Count == 0)
                return "-";

            int min = indices.Min();
            int max = indices.Max();

            return min == max
                ? $"| каналы {min}"
                : $"| каналы. {min}–{max}";
        }

        public override string ToString() => DisplayText;
    }
}