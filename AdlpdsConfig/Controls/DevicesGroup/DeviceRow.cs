using AdlpLib.Models.Devices;
using AdlpLib.Models.Devices.Exec;
using AdlpLib.Models.Devices.PU;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
            DisplayText = device switch
            {
                PuConfigDto pu => $"[PU] [{pu.DeviceType}] [{pu.Block}] {Device.Name} ({pu.Mac})",
                ExecConfigDto ex => $"[EXEC] [{ex.DeviceType}] [{ex.Block}] {device.Name} ({ex.Mac})",
                _ => $"[?]  [{device.Block}] [{device.DeviceType}] {device.Name} ({device.Mac})"
            };
        }

        public override string ToString() => DisplayText;
    }
}
