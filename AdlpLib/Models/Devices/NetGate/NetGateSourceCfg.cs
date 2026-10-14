using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AdlpLib.Models.Devices.NetGate
{
    /// <summary>
    /// Описание источника или назначения сигнала.
    /// </summary>
    public class NetGateSourceCfg
    {
        /// <summary>
        /// Протокол передачи (например, sACN, artnet, dmx).
        /// </summary>
        public string Protocol { get; set; }

        /// <summary>
        /// Номер юниверса.
        /// </summary>
        public int Universe { get; set; }
    }
}
