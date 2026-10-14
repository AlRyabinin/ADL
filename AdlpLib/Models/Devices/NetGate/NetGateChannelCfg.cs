using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AdlpLib.Models.Devices.NetGate
{
    /// <summary>
    /// Конфигурация канала (линии).
    /// </summary>
    public class NetGateChannelCfg
    {
        /// <summary>
        /// Локальный номер канала.
        /// </summary>
        public int Local { get; set; }

        /// <summary>
        /// Имя канала.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Глобальный индекс.
        /// </summary>
        public int Index { get; set; }
    }
}
