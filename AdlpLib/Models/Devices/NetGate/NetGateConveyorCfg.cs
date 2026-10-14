using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AdlpLib.Models.Devices.NetGate
{
    /// <summary>
    /// Конфигурация одного конвейера NetGate.
    /// </summary>
    public class NetGateConveyorCfg
    {
        /// <summary>
        /// Локальный номер конвейера.
        /// </summary>
        public int Local { get; set; }

        /// <summary>
        /// Источник A.
        /// </summary>
        public NetGateSourceCfg SourceA { get; set; }

        /// <summary>
        /// Источник B.
        /// </summary>
        public NetGateSourceCfg SourceB { get; set; }

        /// <summary>
        /// Источник C.
        /// </summary>
        public NetGateSourceCfg SourceC { get; set; }

        /// <summary>
        /// Назначение (выход).
        /// </summary>
        public NetGateSourceCfg Destination { get; set; }

        /// <summary>
        /// Выбор активного источника.
        /// </summary>
        public int SourceSelect { get; set; }

        /// <summary>
        /// Тип суммирования.
        /// </summary>
        public int SumType { get; set; }

        /// <summary>
        /// Частота.
        /// </summary>
        public int Frequency { get; set; }

        /// <summary>
        /// Флаг возможности записи.
        /// </summary>
        public bool Recordable { get; set; }

        /// <summary>
        /// Флаг тайм-аута.
        /// </summary>
        public bool Timeouted { get; set; }

        /// <summary>
        /// Флаг включения RDM.
        /// </summary>
        public bool RdmEnabled { get; set; }

        /// <summary>
        /// Начало диапазона суммирования B.
        /// </summary>
        public int SumBFrom { get; set; }

        /// <summary>
        /// Конец диапазона суммирования B.
        /// </summary>
        public int SumBTo { get; set; }

        /// <summary>
        /// Начало диапазона суммирования C.
        /// </summary>
        public int SumCFrom { get; set; }

        /// <summary>
        /// Конец диапазона суммирования C.
        /// </summary>
        public int SumCTo { get; set; }
    }
}
