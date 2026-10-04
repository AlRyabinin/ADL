using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AdlpLib
{
    [Flags]
    public enum ChannelFlags
    {
        CHSTATUS_AVAILABLE = 0x0001,  // Канал доступен
		CHSTATUS_ON        = 0x0002,  // Канал включён
		CHSTATUS_CTRL      = 0x0004,  // Прямой/диммерный
		CHSTATUS_ERROR     = 0x0008,  // Ошибка

        CHSTATUS_PARTIAL   = 0x0040,  // Частичный статус
        CHSTATUS_WAIT      = 0x0080   // Смена статуса...
    }
}
