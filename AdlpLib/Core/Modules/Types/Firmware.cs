using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AdlpLib.Core.Modules.Types
{
    public class Firmware
    {
        /// <summary>
        /// Получить версию ПО
        /// </summary>
        public virtual string DeviceVersion => "UNKNOWN";

        /// <summary>
        /// Тип устройства, под который создаётся прошивка
        /// </summary>
        public virtual string DeviceType => "UNKNOWN";

        /// <summary>
        /// Получить данные прошивки
        /// </summary>
        /// <returns></returns>
        public virtual byte[] Data => new byte[] { };
    }
}
