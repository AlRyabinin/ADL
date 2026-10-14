using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AdlpLib.Models.Devices.NetGate
{
    /// <summary>
    /// Конфигурация картинки (пресета).
    /// </summary>
    public class NetGatePictureCfg
    {
        /// <summary>
        /// Локальный номер картинки.
        /// </summary>
        public int Local { get; set; }

        /// <summary>
        /// Номер картинки.
        /// </summary>
        public int Picture { get; set; }

        /// <summary>
        /// Время плавного появления.
        /// </summary>
        public int FadeIn { get; set; }

        /// <summary>
        /// Время плавного исчезновения.
        /// </summary>
        public int FadeOut { get; set; }

        /// <summary>
        /// Значение Mux1.
        /// </summary>
        public int Mux1 { get; set; }

        /// <summary>
        /// Значение Mux2.
        /// </summary>
        public int Mux2 { get; set; }

        /// <summary>
        /// Значение Mux3.
        /// </summary>
        public int Mux3 { get; set; }

        /// <summary>
        /// Значение Mux4.
        /// </summary>
        public int Mux4 { get; set; }
    }
}
